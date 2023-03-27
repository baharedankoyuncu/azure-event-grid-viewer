using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using viewer.Service;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : Controller
    {
        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               EventTypes.SubscriptionValidation;

        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               EventTypes.Notification;

        private readonly IEventGridEventHandler _eventGridEventHandler;

        /// <summary>
        /// Constructor for DI.
        /// </summary>
        /// <param name="eventGridEventHandler"></param>
        public UpdatesController(IEventGridEventHandler eventGridEventHandler)
        {
            _eventGridEventHandler = eventGridEventHandler;
        }

        /// <summary>
        /// Endpoint validation request with CloudEvents v1.0.
        /// See https://github.com/cloudevents/spec/blob/v1.0/http-webhook.md#4-abuse-protection
        /// </summary>
        /// <returns></returns>
        [HttpOptions]
        public IActionResult Options()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
                var webhookRequestCallback = HttpContext.Request.Headers["WebHook-Request-Callback"];
                var webhookRequestRate = HttpContext.Request.Headers["WebHook-Request-Rate"];
                HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);
            }

            return Ok();
        }

        /// <summary>
        /// Event subscription endpoint.
        /// Example: https://{{site-name}}.azurewebsites.net/api/updates
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();
            var binaryData = BinaryData.FromString(jsonContent);

            if (EventTypeSubcriptionValidation)
            {
                var eventGridEvent = EventGridEvent.Parse(binaryData);
                var subscriptionValidationEventData = eventGridEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();
                var subscriptionValidationResponse = await _eventGridEventHandler.HandleValidation(eventGridEvent, subscriptionValidationEventData);

                return Ok(subscriptionValidationResponse);
            }
            else if (EventTypeNotification)
            {
                var document = JsonDocument.Parse(jsonContent);
                if (_eventGridEventHandler.IsCloudEvent(document.RootElement))
                {
                    var cloudEvents = CloudEvent.ParseMany(binaryData);
                    await _eventGridEventHandler.HandleCloudEvent(cloudEvents);

                    return Ok();
                }

                var eventGridEvents = EventGridEvent.ParseMany(binaryData);
                await _eventGridEventHandler.HandleGridEvents(eventGridEvents);

                return Ok();
            }

            return BadRequest();
        }
    }
}
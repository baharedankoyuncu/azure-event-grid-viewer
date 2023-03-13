using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using viewer.Hubs;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : Controller
    {
        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "SubscriptionValidation";

        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "Notification";

        private readonly IHubContext<GridEventsHub> _hubContext;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        public UpdatesController(IHubContext<GridEventsHub> gridEventsHubContext)
        {
            this._hubContext = gridEventsHubContext;
        }

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
                var subscriptionValidationResponse = await HandleValidation(eventGridEvent, subscriptionValidationEventData);

                return Ok(subscriptionValidationResponse);
            }
            else if (EventTypeNotification)
            {
                var document = JsonDocument.Parse(jsonContent);
                if (IsCloudEvent(document.RootElement))
                {
                    var cloudEvents = CloudEvent.ParseMany(binaryData);
                    await HandleCloudEvent(cloudEvents);

                    return Ok();
                }

                var eventGridevents = EventGridEvent.ParseMany(binaryData);
                await HandleGridEvents(eventGridevents);

                return Ok();
            }

            return BadRequest();
        }

        /// <summary>
        /// Proves endpoint ownership via echo of validation code.
        /// Validation event has same body as Event Grid events.
        /// </summary>
        /// <param name="eventGridEvent">Event Grid event to publish metadata</param>
        /// <param name="validationEventData">Validation event containing validation code</param>
        /// <returns></returns>
        private async Task<SubscriptionValidationResponse> HandleValidation(EventGridEvent eventGridEvent, SubscriptionValidationEventData validationEventData)
        {
            await this._hubContext.Clients.All.SendAsync(
                "gridupdate",
                eventGridEvent.Id,
                eventGridEvent.EventType,
                eventGridEvent.Subject,
                eventGridEvent.EventTime,
                JsonSerializer.Serialize(eventGridEvent, _jsonSerializerOptions));

            return new SubscriptionValidationResponse
            {
                ValidationResponse = validationEventData.ValidationCode
            };
        }

        /// <summary>
        /// Publishes Event Grid events to the client.
        /// </summary>
        /// <param name="eventGridEvents">Events to be published</param>
        private async Task HandleGridEvents(IEnumerable<EventGridEvent> eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                await this._hubContext.Clients.All.SendAsync(
                    "gridupdate",
                    eventGridEvent.Id,
                    eventGridEvent.EventType,
                    eventGridEvent.Subject,
                    eventGridEvent.EventTime,
                    JsonSerializer.Serialize(eventGridEvent, _jsonSerializerOptions));
            }
        }

        /// <summary>
        /// Publishes Cloud Event events to the client.
        /// </summary>
        /// <param name="cloudEvents">Events to be published</param>
        private async Task HandleCloudEvent(IEnumerable<CloudEvent> cloudEvents)
        {
            foreach (var cloudEvent in cloudEvents)
            {
                await this._hubContext.Clients.All.SendAsync(
                "gridupdate",
                cloudEvent.Id,
                cloudEvent.Type,
                cloudEvent.Subject,
                cloudEvent.Time,
                JsonSerializer.Serialize(cloudEvent, _jsonSerializerOptions));
            }
        }

        /// <summary>
        /// Checks whether the event is of type <see cref="CloudEvent"/>. 
        /// </summary>
        /// <param name="jsonElement">The JSON element representation of the data</param>
        /// <returns></returns>
        private static bool IsCloudEvent(JsonElement jsonElement)
        {
            const string cloudEventProperty = "specversion";

            try
            {
                return jsonElement.TryGetProperty(cloudEventProperty, out var _);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }
    }
}
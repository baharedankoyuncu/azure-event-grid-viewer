using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using viewer.Hubs;

namespace viewer.Service
{
    public class EventGridEventHandler : IEventGridEventHandler
    {
        private readonly IHubContext<GridEventsHub> _hubContext;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        /// <summary>
        /// Constructor for DI.
        /// </summary>
        /// <param name="hubContext"></param>
        public EventGridEventHandler(IHubContext<GridEventsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Proves endpoint ownership via echo of validation code.
        /// Validation event has same body as Event Grid events.
        /// </summary>
        /// <param name="eventGridEvent">Event Grid event to publish metadata</param>
        /// <param name="validationEventData">Validation event containing validation code</param>
        /// <returns></returns>
        public async Task<SubscriptionValidationResponse> HandleValidation(EventGridEvent eventGridEvent, SubscriptionValidationEventData validationEventData)
        {
            await _hubContext.Clients.All.SendAsync(
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
        public async Task HandleGridEvents(IEnumerable<EventGridEvent> eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                await _hubContext.Clients.All.SendAsync(
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
        public async Task HandleCloudEvent(IEnumerable<CloudEvent> cloudEvents)
        {
            foreach (var cloudEvent in cloudEvents)
            {
                await _hubContext.Clients.All.SendAsync(
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
        public bool IsCloudEvent(JsonElement jsonElement)
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

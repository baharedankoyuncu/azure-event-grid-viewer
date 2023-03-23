using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace viewer.Service
{
    public interface IEventGridEventHandler
    {
        Task HandleCloudEvent(IEnumerable<CloudEvent> cloudEvents);

        Task HandleGridEvents(IEnumerable<EventGridEvent> eventGridEvents);

        Task<SubscriptionValidationResponse> HandleValidation(EventGridEvent eventGridEvent, SubscriptionValidationEventData validationEventData);

        bool IsCloudEvent(JsonElement jsonElement);
    }
}
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using viewer.Hubs;
using viewer.Service;

namespace Test
{
    public class EventGridCloudEventsShould
    {
        [Fact]
        public void ReturnTrueWhenEventContainsSpecVersionProperty()
        {
            // Arrange
            var json = EmbeddedResource.Get("Valid_CloudEvent.json");
            var fakeContext = A.Fake<IHubContext<GridEventsHub>>();
            var eventGridHandler = new EventGridEventHandler(fakeContext);
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Act
            var isCloudEvent = eventGridHandler.IsCloudEvent(jsonElement);

            // Assert
            isCloudEvent.Should().BeTrue();
        }

        [Fact]
        public void ReturnFalseWhenEventDoesNotContainsSpecVersionProperty()
        {
            // Arrange
            var json = EmbeddedResource.Get("CloudEvent_Missing_SpecVersion.json");
            var fakeContext = A.Fake<IHubContext<GridEventsHub>>();
            var eventGridHandler = new EventGridEventHandler(fakeContext);
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Act
            var isCloudEvent = eventGridHandler.IsCloudEvent(jsonElement);

            // Assert
            isCloudEvent.Should().BeFalse();
        }

        [Fact]
        public void ReturnFalseWhenEventPayloadIsInvalid()
        {
            // Arrange
            var fakeContext = A.Fake<IHubContext<GridEventsHub>>();
            var eventGridHandler = new EventGridEventHandler(fakeContext);

            // Act
            var isCloudEvent = eventGridHandler.IsCloudEvent(default);

            // Assert
            isCloudEvent.Should().BeFalse();
        }
    }
}

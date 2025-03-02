using System.Text.Json;

namespace Basses.SimpleEventStore.EventStore;

public class DefaultEventSerializer : IEventSerializer
{
    public object Deserialize(string eventPayload, string eventType)
    {
        var type = Type.GetType(eventType);
        if (type == null)
        {
            throw new EventStoreException($"Unknown event type: {eventType}");
        }

        var @event = JsonSerializer.Deserialize(eventPayload, type);
        if (@event == null)
        {
            throw new EventStoreException($"Deserialization failed for event type: {eventType}");
        }

        return @event;
    }

    public SerializerResult Serialize(object @event)
    {
        var eventPayload = JsonSerializer.Serialize(@event);
        var eventType = @event.GetType().AssemblyQualifiedName ?? "";
        return new SerializerResult(eventPayload, eventType);
    }
}


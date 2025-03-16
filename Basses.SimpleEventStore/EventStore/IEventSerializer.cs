namespace Basses.SimpleEventStore.EventStore;

public interface IEventSerializer
{
    SerializerResult Serialize(object @event);
    object Deserialize(string eventPayload, string eventType);
}

public record SerializerResult(string EventPayload, string EventType);


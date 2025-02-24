namespace Basses.SimpleEventStore.EventStore;

public record EventEntry(
    string SequenceNumber,
    Guid StreamId,
    int Version,
    DateTimeOffset Timestamp,
    string EventType,
    object Event
);

namespace Basses.SimpleEventStore.EventStore;

public record EventEntry(
    long SequenceNumber,
    string StreamId,
    int Version,
    DateTimeOffset Timestamp,
    string EventType,
    object Event
);

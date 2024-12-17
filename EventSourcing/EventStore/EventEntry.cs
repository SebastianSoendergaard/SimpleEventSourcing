namespace EventSourcing.EventStore;

public record EventEntry(long SequenceNumber, Guid StreamId, int Version, DateTimeOffset Timestamp, string EventType, object Event);

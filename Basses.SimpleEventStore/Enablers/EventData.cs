namespace Basses.SimpleEventStore.Enablers;

public record EventData(long SequenceNumber, string StreamId, int Version, DateTimeOffset Timestamp);

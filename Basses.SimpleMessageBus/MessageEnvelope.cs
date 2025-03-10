namespace Basses.SimpleMessageBus;

public class MessageEnvelope
{
    public required string MessageName { get; init; }
    public required string ProducerName { get; init; }
    public required DateTimeOffset PublishTime { get; init; }
    public required int Version { get; init; }
    public string? DeprecationMessage { get; init; }
    public required object Payload { get; init; }
}

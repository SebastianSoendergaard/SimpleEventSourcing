namespace UnderstandingEventsourcingExample.Cart.Infrastructure.Kafka;

public class KafkaOptions
{
    public required string Server { get; init; }
    public required string ClientId { get; init; }
    public required string GroupId { get; init; }
    public required string ProducerName { get; init; }
}

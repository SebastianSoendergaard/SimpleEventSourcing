namespace UnderstandingEventsourcingExample.Loyalty
{
    public class LoyaltyOptions
    {
        public required string ConnectionString { get; init; }
        public required string Schema { get; init; }
        public required string EventStoreName { get; init; }
    }
}

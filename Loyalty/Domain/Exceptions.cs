namespace UnderstandingEventsourcingExample.Loyalty.Domain;

public class LoyaltyException : Exception
{
    public LoyaltyException(string message)
        : base(message)
    {
    }
}

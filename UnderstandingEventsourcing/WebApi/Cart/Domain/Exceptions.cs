namespace UnderstandingEventsourcingExample.Cart.Domain;

public class CartException : Exception
{
    public CartException(string message)
        : base(message)
    {
    }
}

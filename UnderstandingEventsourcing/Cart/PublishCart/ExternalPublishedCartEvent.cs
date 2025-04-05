namespace UnderstandingEventsourcingExample.Cart.PublishCart;

public record ExternalPublishedCartEvent(
    Guid CartId,
    ExternalPublishedCartEvent.OrderedProduct[] OrderedProducts,
    decimal TotalPrice
)
{
    public record OrderedProduct(Guid ProductId, decimal Price);
};


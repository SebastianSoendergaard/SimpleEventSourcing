namespace UnderstandingEventsourcingExample.Cart.PublishCart;

public record ExternalOrderedProduct(Guid ProductId, decimal Price);
public record ExternalPublishedCartEvent(
    Guid CartId,
    ExternalOrderedProduct[] OrderedProducts,
    decimal TotalPrice
);


namespace UnderstandingEventsourcingExample.Cart.GetCartItems;

public record CartItemsReadModel(
    Guid CartId,
    decimal TotalPrice,
    IEnumerable<CartItem> Items
);

public record CartItem(
    Guid CartId,
    string Description,
    string Image,
    decimal Price,
    Guid ItemId,
    Guid ProductId
);


namespace UnderstandingEventsourcingExample.Cart.GetCartsWithProducts;

public record CartProduct(
    Guid CartId,
    Guid ProductId
);

public record GetCartsWithProductsReadModel(
    IEnumerable<CartProduct> CartsWithProducts
);


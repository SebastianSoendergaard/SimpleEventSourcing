using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetCartsWithProducts;

public record GetCartsWithProductsQuery(
    Guid ProductId
);

public class GetCartsWithProductsQueryHandler(GetCartsWithProductsProjector projector)
{
    public async Task<GetCartsWithProductsReadModel> Handle(GetCartsWithProductsQuery query)
    {
        var readModel = await projector.GetReadModel(query.ProductId);
        if (readModel == null)
        {
            throw new CartException("Cart with products read model can not be found");
        }

        return readModel;
    }
}

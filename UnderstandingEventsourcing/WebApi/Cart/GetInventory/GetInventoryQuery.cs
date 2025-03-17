using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetInventory;

public record GetInventoryQuery(
    Guid ProductId
);

public class GetInventoryQueryHandler(GetInventoryProjector projector)
{
    public async Task<InventoryReadModel> Handle(GetInventoryQuery query)
    {
        var readModel = await projector.GetReadModel(query.ProductId);
        if (readModel == null)
        {
            throw new CartException("Inventories read model can not be found");
        }

        return readModel;
    }
}

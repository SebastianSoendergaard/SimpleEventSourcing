using Basses.SimpleEventStore.EventStore;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetCartItems;

public record GetProductInventoryQuery(
    Guid ProductId
);

public class GetProductInventoryQueryHandler(IEventStore eventStore)
{
    public async Task<CartItemsReadModel> Handle(GetProductInventoryQuery query)
    {
        var events = await eventStore.LoadEvents(query.ProductId);

        var projector = new CartItemsProjector();
        await projector.Update(events);

        var readModel = projector.GetReadModel();
        if (readModel == null)
        {
            throw new CartException("Cart items read model can not be found");
        }

        return readModel;
    }
}

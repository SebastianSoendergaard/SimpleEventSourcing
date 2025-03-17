using Basses.SimpleEventStore.EventStore;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetCartItems;

public record GetCartItemsQuery(
    Guid CartId
);

public class GetCartItemsQueryHandler(IEventStore eventStore)
{
    public async Task<CartItemsReadModel> Handle(GetCartItemsQuery query)
    {
        var events = await eventStore.LoadEvents(query.CartId.ToString());

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

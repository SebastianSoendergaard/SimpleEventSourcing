using Basses.SimpleEventStore.Enablers;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.GetCartsWithProducts;

namespace UnderstandingEventsourcingExample.Cart.ArchiveItem;

public class ArchiveItemAutomationReactor(GetCartsWithProductsQueryHandler queryHandler, ArchiveItemCommandHandler commandHandler) : Reactor,
    IReactionEventHandler<PriceChangedEvent>
{
    public async Task ReactOn(PriceChangedEvent @event, EventData eventData)
    {
        var readModel = await queryHandler.Handle(new GetCartsWithProductsQuery(@event.ProductId));

        foreach (var cart in readModel.CartsWithProducts)
        {
            var cmd = new ArchiveItemCommand(cart.CartId, cart.ProductId);

            await commandHandler.Handle(cmd);
        }
    }
}

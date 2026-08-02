using Basses.SimpleEventStore.Enablers;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.PublishCart;

public class PublishCartAutomationReactor(PublishCartCommandHandler handler) : Reactor,
    IReactionEventHandler<CartSubmittedEvent>
{
    public Task ReactOn(CartSubmittedEvent @event, EventData eventData)
    {
        var cmd = new PublishCartCommand(
            CartId: @event.CartId,
            OrderedProducts: @event.OrderedProducts.Select(x => new PublishCartCommand.OrderedProduct(ProductId: x.ProductId, Price: x.Price)).ToArray(),
            TotalPrice: @event.TotalPrice
        );

        return handler.Handle(cmd);
    }
}

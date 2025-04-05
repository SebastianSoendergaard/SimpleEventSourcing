using Basses.SimpleEventStore.Enablers;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.PublishCart;

internal class PublishCartAutomationReactor(PublishCartCommandHandler handler) : Reactor,
    IReactionEventHandler<CartSubmittedEvent>
{
    public Task ReactOn(CartSubmittedEvent @event, EventData eventData)
    {
        var cmd = new PublishCartCommand(
            @event.CartId,
            @event.OrderedProducts.Select(x => new PublishCartCommand.OrderedProduct(x.ProductId, x.Price)).ToArray(),
            @event.TotalPrice
        );

        return handler.Handle(cmd);
    }
}

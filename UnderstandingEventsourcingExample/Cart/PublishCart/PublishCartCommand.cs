using Basses.SimpleMessageBus;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.PublishCart;

public record OrderedProduct(Guid ProductId, decimal Price);
public record PublishCartCommand(
    Guid CartId,
    OrderedProduct[] OrderedProducts,
    decimal TotalPrice
);

public class PublishCartCommandHandler(CartRepository repository, IMessageProducer messageProducer)
{
    public async Task Handle(PublishCartCommand command)
    {
        var cart = await repository.TryGet(command.CartId.ToString());
        if (cart == null)
        {
            throw new CartException("Cart does not exist");
        }

        var externalEvent = new ExternalPublishedCartEvent(
            command.CartId,
            command.OrderedProducts.Select(p => new ExternalOrderedProduct(p.ProductId, p.Price)).ToArray(),
            command.TotalPrice
        );

        var result = await messageProducer.SendMessage("understand-eventsourcing-topic", "cart-published", externalEvent);
        if (result)
        {
            cart.Publish();
        }
        else
        {
            cart.FailPublication();
        }

        await repository.Update(cart);
    }
}

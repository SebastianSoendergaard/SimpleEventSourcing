using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.ArchiveItem;

public record ArchiveItemCommand(
    Guid CartId,
    Guid ProductId
);

public class ArchiveItemCommandHandler(CartRepository repository)
{
    public async Task Handle(ArchiveItemCommand command)
    {
        var cart = await repository.TryGet(command.CartId.ToString());
        if (cart == null)
        {
            throw new CartException("Cart does not exist");
        }

        cart.ArchiveItem(command.ProductId);
        await repository.Update(cart);
    }
}

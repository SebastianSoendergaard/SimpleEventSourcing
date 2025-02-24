using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.ClearCart;

public record ClearCartCommand(
    Guid CartId
);

public class ClearCartCommandHandler(CartRepository repository)
{
    public async Task Handle(ClearCartCommand command)
    {
        var cart = await repository.TryGet(command.CartId.ToString());
        if (cart == null)
        {
            throw new CartException("Cart does not exist");
        }

        cart.Clear();
        await repository.Update(cart);
    }
}

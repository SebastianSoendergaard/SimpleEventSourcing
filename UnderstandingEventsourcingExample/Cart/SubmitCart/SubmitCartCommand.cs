using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.ClearCart;

public record SubmitCartCommand(
    Guid CartId
);

public class SubmitCartCommandHandler(CartRepository repository)
{
    public async Task Handle(SubmitCartCommand command)
    {
        var cart = await repository.TryGet(command.CartId.ToString());
        if (cart == null)
        {
            throw new CartException("Cart does not exist");
        }

        cart.Submit();
        await repository.Update(cart);
    }
}

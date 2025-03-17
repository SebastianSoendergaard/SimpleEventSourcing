using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.ClearCart;

public record ChangePriceCommand(
    Guid ProductId,
    decimal NewPrice,
    decimal OldPrice
);

public class ChangePriceCommandHandler(PriceRepository repository)
{
    public async Task Handle(ChangePriceCommand command)
    {
        var price = await repository.TryGet(command.ProductId.ToString());
        if (price == null)
        {
            price = new PriceAggregate(command.ProductId, command.NewPrice, command.OldPrice);
            await repository.Add(price);
        }
        else
        {
            price.Update(command.NewPrice, command.OldPrice);
            await repository.Update(price);
        }
    }
}

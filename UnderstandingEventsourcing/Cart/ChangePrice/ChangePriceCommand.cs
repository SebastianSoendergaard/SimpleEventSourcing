using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.ClearCart;

public record ChangePriceCommand(
    Guid ProductId,
    decimal NewPrice,
    decimal OldPrice
);

public class ChangePriceCommandHandler(PricingRepository repository)
{
    public async Task Handle(ChangePriceCommand command)
    {
        var pricingId = PricingAggregate.CreatePricingIdFromGuid(command.ProductId);
        var price = await repository.TryGet(pricingId);
        if (price == null)
        {
            price = new PricingAggregate(command.ProductId, command.NewPrice, command.OldPrice);
            await repository.Add(price);
        }
        else
        {
            price.Update(command.NewPrice, command.OldPrice);
            await repository.Update(price);
        }
    }
}

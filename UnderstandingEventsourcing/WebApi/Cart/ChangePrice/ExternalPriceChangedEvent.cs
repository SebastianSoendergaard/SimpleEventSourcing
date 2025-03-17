namespace UnderstandingEventsourcingExample.Cart.ChangePrice;

public record ExternalPriceChangedEvent(
    Guid ProductId,
    decimal NewPrice,
    decimal OldPrice
);

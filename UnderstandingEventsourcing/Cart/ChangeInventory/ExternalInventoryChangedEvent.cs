namespace UnderstandingEventsourcingExample.Cart.ChangeInventory;

public record ExternalInventoryChangedEvent(
    Guid ProductId,
    int Inventory
);

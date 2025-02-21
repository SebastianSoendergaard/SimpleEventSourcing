namespace UnderstandingEventsourcingExample.Cart.GetInventory;

public record InventoryReadModel(
    Guid ProductId,
    int Inventory
);


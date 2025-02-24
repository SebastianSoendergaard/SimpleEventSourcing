using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.ChangeInventory;

public record ChangeInventoryCommand(
    Guid ProductId,
    int Inventory
);

public class ChangeInventoryCommandHandler(InventoryRepository repository)
{
    public async Task Handle(ChangeInventoryCommand command)
    {
        var inventory = await repository.TryGet(command.ProductId.ToString());
        if (inventory == null)
        {
            inventory = new InventoryAggregate(command.ProductId, command.Inventory);
            await repository.Add(inventory);
        }
        else
        {
            inventory.Update(command.Inventory);
            await repository.Update(inventory);
        }
    }
}

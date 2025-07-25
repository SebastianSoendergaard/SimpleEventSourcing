using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class InventoryAggregate : Aggregate,
    IDomainEventHandler<InventoryChangedEvent>
{
    public InventoryAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public InventoryAggregate(Guid productId, int inventory)
    {
        Apply(new InventoryChangedEvent(productId, inventory));
    }

    public void Update(int inventory)
    {
        var id = CreateGuidFromInventoryId(Id);
        Apply(new InventoryChangedEvent(id, inventory));
    }

    public void On(InventoryChangedEvent @event)
    {
        Id = CreateInventoryIdFromGuid(@event.ProductId);
    }

    public static string CreateInventoryIdFromGuid(Guid guid)
    {
        return $"inventory-{guid}";
    }

    public static Guid CreateGuidFromInventoryId(string inventoryId)
    {
        var id = inventoryId.Replace("inventory-", "");
        return new Guid(id);
    }
}

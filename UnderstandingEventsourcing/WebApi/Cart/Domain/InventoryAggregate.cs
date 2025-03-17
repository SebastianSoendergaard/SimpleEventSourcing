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
        Apply(new InventoryChangedEvent(new Guid(Id), inventory));
    }

    public void On(InventoryChangedEvent @event)
    {
        Id = @event.ProductId.ToString();
    }
}

using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class PriceAggregate : Aggregate,
    IDomainEventHandler<PriceChangedEvent>
{
    public PriceAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public PriceAggregate(Guid productId, decimal newPrice, decimal oldPrice)
    {
        Apply(new PriceChangedEvent(productId, newPrice, oldPrice));
    }

    public void Update(decimal newPrice, decimal oldPrice)
    {
        Apply(new PriceChangedEvent(new Guid(Id), newPrice, oldPrice));
    }

    public void On(PriceChangedEvent @event)
    {
        Id = @event.ProductId.ToString();
    }
}

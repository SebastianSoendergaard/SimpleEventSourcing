using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class PricingAggregate : Aggregate,
    IDomainEventHandler<PriceChangedEvent>
{
    public PricingAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public PricingAggregate(Guid productId, decimal newPrice, decimal oldPrice)
    {
        Apply(new PriceChangedEvent(productId, newPrice, oldPrice));
    }

    public void Update(decimal newPrice, decimal oldPrice)
    {
        var id = CreateGuidFromPricingId(Id);
        Apply(new PriceChangedEvent(id, newPrice, oldPrice));
    }

    public void On(PriceChangedEvent @event)
    {
        Id = CreatePricingIdFromGuid(@event.ProductId);
    }

    public static string CreatePricingIdFromGuid(Guid guid)
    {
        return $"pricing-{guid}";
    }

    public static Guid CreateGuidFromPricingId(string pricingId)
    {
        var id = pricingId.Replace("pricing-", "");
        return new Guid(id);
    }
}

using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class PricingRepository : EventSourcedRepository<PricingAggregate>
{
    public PricingRepository(IEventStore eventStore) : base(eventStore)
    {
    }
}

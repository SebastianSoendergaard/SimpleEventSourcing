using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class PriceRepository : EventSourcedRepository<PriceAggregate>
{
    public PriceRepository(IEventStore eventStore) : base(eventStore)
    {
    }
}

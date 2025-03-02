using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class InventoryRepository : EventSourcedRepository<InventoryAggregate>
{
    public InventoryRepository(IEventStore eventStore) : base(eventStore)
    {
    }
}

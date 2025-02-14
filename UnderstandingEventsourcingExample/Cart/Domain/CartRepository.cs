using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class CartRepository : EventSourcedRepository<CartAggregate>
{
    public CartRepository(IEventStore eventStore) : base(eventStore)
    {
    }
}

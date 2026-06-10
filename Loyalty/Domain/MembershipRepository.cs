using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

public class MembershipRepository : EventSourcedRepository<MembershipAggregate>
{
    public MembershipRepository(IEventStore eventStore) : base(eventStore)
    {
    }
}

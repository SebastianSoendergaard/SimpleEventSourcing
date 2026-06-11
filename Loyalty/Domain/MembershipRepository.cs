using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

internal class MembershipRepository : EventSourcedRepository<MembershipAggregate>
{
    public MembershipRepository(IEventStore eventStore) : base(eventStore)
    {
    }
}

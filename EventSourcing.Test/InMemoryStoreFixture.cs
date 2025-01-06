using EventSourcing.EventStore;
using EventSourcing.EventStore.InMemory;

namespace EventSourcing.Test;

public class InMemoryStoreFixture
{
    public IEventStore CreateEventStore()
    {
        return new InMemoryEventStore();
    }
}

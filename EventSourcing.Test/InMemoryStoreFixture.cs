using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;

namespace EventSourcing.Test;

public class InMemoryStoreFixture
{
    public IEventStore CreateEventStore()
    {
        return new InMemoryEventStore();
    }
}

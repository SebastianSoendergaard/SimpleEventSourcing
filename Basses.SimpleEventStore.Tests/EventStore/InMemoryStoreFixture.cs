using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;

namespace Basses.SimpleEventStore.Tests.EventStore;

public class InMemoryStoreFixture
{
    public IEventStore CreateEventStore()
    {
        return new InMemoryEventStore();
    }
}

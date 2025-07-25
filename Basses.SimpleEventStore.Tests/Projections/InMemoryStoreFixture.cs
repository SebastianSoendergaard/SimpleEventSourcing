using Basses.SimpleEventStore.Projections;
using Basses.SimpleEventStore.Projections.InMemory;

namespace Basses.SimpleEventStore.Tests.Projections;

public class InMemoryStoreFixture
{
    public IProjectorStateStore CreateEventStore()
    {
        return new InMemoryProjectorStateStore();
    }
}

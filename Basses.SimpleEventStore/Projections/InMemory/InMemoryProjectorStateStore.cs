using Basses.SimpleEventStore.EventSubscriber.InMemory;

namespace Basses.SimpleEventStore.Projections.InMemory;

public class InMemoryProjectorStateStore : InMemoryEventSubscriberStateStore, IProjectorStateStore
{
    public InMemoryProjectorStateStore() : base()
    {
    }
}

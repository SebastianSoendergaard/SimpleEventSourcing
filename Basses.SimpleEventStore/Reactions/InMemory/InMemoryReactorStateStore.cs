using Basses.SimpleEventStore.EventSubscriber.InMemory;

namespace Basses.SimpleEventStore.Reactions.InMemory;

public class InMemoryReactorStateStore : InMemoryEventSubscriberStateStore, IReactorStateStore
{
    public InMemoryReactorStateStore() : base()
    {
    }
}

using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;

namespace Basses.SimpleEventStore.Reactions;

public class ReactionManager : EventSubscriptionManager
{
    public ReactionManager(IEventStore eventStore, IReactorStateStore reactorStateStore, IServiceProvider serviceProvider)
        : base(eventStore, reactorStateStore, serviceProvider)
    {
    }

    public ReactionManager RegisterSynchronousReactor<TReactor>() where TReactor : IReactor
    {
        RegisterSynchronousSubscriber<TReactor>();
        return this;
    }

    public ReactionManager RegisterAsynchronousReactor<TReactor>() where TReactor : IReactor
    {
        RegisterAsynchronousSubscriber<TReactor>();
        return this;
    }

    public IEnumerable<Type> GetReactorTypes()
    {
        return GetSubscriberTypes();
    }

    public Task<EventSubscriberProcessingState> GetProcessingState(IReactor reactor)
    {
        return base.GetProcessingState(reactor);
    }
}

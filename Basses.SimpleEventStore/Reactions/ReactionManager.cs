using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;
using Microsoft.Extensions.Logging;

namespace Basses.SimpleEventStore.Reactions;

public class ReactionManager : EventSubscriptionManager
{
    public ReactionManager(IEventStore eventStore, IReactorStateStore reactorStateStore, ReactionsRegister reactionsRegister, IServiceProvider serviceProvider)
        : base(eventStore, reactorStateStore, reactionsRegister, serviceProvider)
    {
    }

    public IEnumerable<Type> GetReactorTypes()
    {
        return GetSubscriberTypes();
    }

    public Task<EventSubscriberProcessingState> GetProcessingState(IReactor reactor)
    {
        return base.GetProcessingState(reactor);
    }

    public Task RunAsync(ILogger logger, CancellationToken stoppingToken)
    {
        return NotifyAsynchronousSubscribers(logger, stoppingToken);
    }
}

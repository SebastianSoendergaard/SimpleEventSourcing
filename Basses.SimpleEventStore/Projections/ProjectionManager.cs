using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;
using Microsoft.Extensions.Logging;

namespace Basses.SimpleEventStore.Projections;

public class ProjectionManager : EventSubscriptionManager
{
    public ProjectionManager(IEventStore eventStore, IProjectorStateStore projectorStateStore, ProjectionsRegister projectionsRegister, IInstrumentation instrumentation, IServiceProvider serviceProvider)
        : base(eventStore, projectorStateStore, projectionsRegister, instrumentation, serviceProvider)
    {
    }

    public IEnumerable<Type> GetProjectorTypes()
    {
        return GetSubscriberTypes();
    }

    public Task<EventSubscriberProcessingState> GetProcessingState(IProjector projector)
    {
        return base.GetProcessingState(projector);
    }

    public Task RunAsync(ILogger logger, CancellationToken stoppingToken)
    {
        return NotifyAsynchronousSubscribers(logger, stoppingToken);
    }
}

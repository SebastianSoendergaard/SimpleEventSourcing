using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;

namespace Basses.SimpleEventStore.Projections;

public class ProjectionManager : EventSubscriptionManager
{
    public ProjectionManager(IEventStore eventStore, IProjectorStateStore projectorStateStore, ProjectionsRegister projectionsRegister, IServiceProvider serviceProvider)
        : base(eventStore, projectorStateStore, projectionsRegister, serviceProvider)
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

    public Task RunAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}

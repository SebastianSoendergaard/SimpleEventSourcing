using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;

namespace Basses.SimpleEventStore.Projections;

public class ProjectionManager : EventSubscriptionManager
{
    public ProjectionManager(IEventStore eventStore, IProjectorStateStore projectorStateStore, IServiceProvider serviceProvider)
        : base(eventStore, projectorStateStore, serviceProvider)
    {
    }

    public ProjectionManager RegisterSynchronousProjector<TProjector>() where TProjector : IProjector
    {
        RegisterSynchronousSubscriber<TProjector>();
        return this;
    }

    public ProjectionManager RegisterAsynchronousProjector<TProjector>() where TProjector : IProjector
    {
        RegisterAsynchronousSubscriber<TProjector>();
        return this;
    }

    public IEnumerable<Type> GetProjectorTypes()
    {
        return GetSubscriberTypes();
    }

    public Task<EventSubscriberProcessingState> GetProcessingState(IProjector projector)
    {
        return base.GetProcessingState(projector);
    }
}

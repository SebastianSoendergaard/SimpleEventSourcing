using Basses.SimpleEventStore.EventSubscriber;

namespace Basses.SimpleEventStore.Projections;

public class ProjectionsRegister : SubscriberRegister
{
    internal ProjectionsRegister() { }

    public ProjectionsRegister RegisterSynchronousProjector<TProjector>() where TProjector : IProjector
    {
        RegisterSynchronousSubscriber<TProjector>();
        return this;
    }

    public ProjectionsRegister RegisterAsynchronousProjector<TProjector>() where TProjector : IProjector
    {
        RegisterAsynchronousSubscriber<TProjector>();
        return this;
    }
}

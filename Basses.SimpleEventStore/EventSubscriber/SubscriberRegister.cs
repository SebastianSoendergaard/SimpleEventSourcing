namespace Basses.SimpleEventStore.EventSubscriber;

public abstract class SubscriberRegister
{
    private readonly List<Type> _synchronousSubscribers = [];
    private readonly List<Type> _asynchronousSubscribers = [];

    protected void RegisterSynchronousSubscriber<TSubscriber>() where TSubscriber : IEventSubscriber
    {
        _synchronousSubscribers.Add(typeof(TSubscriber));
    }

    protected void RegisterAsynchronousSubscriber<TSubscriber>() where TSubscriber : IEventSubscriber
    {
        _asynchronousSubscribers.Add(typeof(TSubscriber));
    }

    internal IEnumerable<Type> SynchronousSubscribers { get { return _synchronousSubscribers; } }
    internal IEnumerable<Type> AsynchronousSubscribers { get { return _asynchronousSubscribers; } }
    internal IEnumerable<Type> AllSubscribers { get { return _synchronousSubscribers.Concat(_asynchronousSubscribers).ToList(); } }
}

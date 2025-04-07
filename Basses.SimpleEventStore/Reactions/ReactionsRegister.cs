using Basses.SimpleEventStore.EventSubscriber;

namespace Basses.SimpleEventStore.Reactions;

public class ReactionsRegister : SubscriberRegister
{
    internal ReactionsRegister() { }

    public ReactionsRegister RegisterSynchronousReactor<TReactor>() where TReactor : IReactor
    {
        RegisterSynchronousSubscriber<TReactor>();
        return this;
    }

    public ReactionsRegister RegisterAsynchronousReactor<TReactor>() where TReactor : IReactor
    {
        RegisterAsynchronousSubscriber<TReactor>();
        return this;
    }
}

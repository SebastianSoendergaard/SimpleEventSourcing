using Basses.SimpleEventStore.EventSubscriber;

namespace Basses.SimpleEventStore.Reactions;

public class ReactionsRegister : SubscriberRegister
{
    internal ReactionsRegister() { }

    public ReactionsRegister RegisterAsynchronousReactor<TReactor>() where TReactor : IReactor
    {
        RegisterAsynchronousSubscriber<TReactor>();
        return this;
    }
}

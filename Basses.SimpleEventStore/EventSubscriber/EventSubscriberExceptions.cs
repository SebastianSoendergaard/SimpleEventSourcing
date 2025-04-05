namespace Basses.SimpleEventStore.EventSubscriber;

public class EventSubscriberException : Exception
{
    public EventSubscriberException(string message) : base(message) { }
    public EventSubscriberException(string message, Exception exception) : base(message, exception) { }
}

public class NotFoundException : EventSubscriberException
{
    public NotFoundException(string message) : base(message) { }
}

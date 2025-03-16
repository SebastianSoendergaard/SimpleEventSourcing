namespace Basses.SimpleEventStore.EventStore;

public class EventStoreException : Exception
{
    public EventStoreException(string message) : base(message) { }
    public EventStoreException(string message, Exception exception) : base(message, exception) { }
}

public class VersionConflictException : EventStoreException
{
    public VersionConflictException(string message) : base(message) { }
}

public class NotFoundException : EventStoreException
{
    public NotFoundException(string message) : base(message) { }
}

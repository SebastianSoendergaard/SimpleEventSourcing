namespace Basses.SimpleEventStore.Enablers;

public interface IProjectionEventHandler<T> where T : IDomainEvent
{
    Task UpdateWith(T @event, EventData eventData);
}

namespace Basses.SimpleEventStore.Enablers;

public interface IReactionEventHandler<T> where T : IDomainEvent
{
    Task ReactOn(T @event, EventData eventData);
}

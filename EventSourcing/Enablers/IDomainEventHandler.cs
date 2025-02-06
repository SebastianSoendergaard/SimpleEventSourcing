namespace EventSourcing.Enablers;

public interface IDomainEventHandler<T> where T : IDomainEvent
{
    void On(T @event);
}

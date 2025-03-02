namespace Basses.SimpleEventStore.Enablers;

public abstract class Aggregate
{
    private readonly List<IDomainEvent> _uncommitedDomainEvents = [];

    public IReadOnlyList<IDomainEvent> UncommitedDomainEvents => _uncommitedDomainEvents;
    public int Version { get; private set; }

    public Guid Id { get; protected set; }

    public Aggregate() { }

    public Aggregate(IEnumerable<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            Mutate(@event);
            Version++;
        }
    }

    public void ClearDomainEvents()
    {
        Version += _uncommitedDomainEvents.Count;
        _uncommitedDomainEvents.Clear();
    }

    public void Apply(IDomainEvent @event)
    {
        _uncommitedDomainEvents.Add(@event);
        Mutate(@event);
    }

    private void Mutate(IDomainEvent @event)
    {
        if (MutationRegistry.CanMutate(this, @event, typeof(IDomainEventHandler<>)))
        {
            ((dynamic)this).On((dynamic)@event);
        }
    }
}

namespace WebApi.Domain;

public abstract class Aggregate
{
    private readonly List<IDomainEvent> _uncommitedDomainEvents = [];

    public IReadOnlyList<IDomainEvent> UncommitedDomainEvents => _uncommitedDomainEvents;
    public int Version { get; private set; }

    public Guid Id { get; protected set; }

    public Aggregate() { }

    public Aggregate(IEnumerable<IDomainEvent> events)
    {
        foreach (IDomainEvent @event in events)
        {
            Mutate(@event);
            Version++;
        }
    }

    public void ClearDomainEvents()
    {
        _uncommitedDomainEvents.Clear();
    }

    public void Apply(IDomainEvent @event)
    {
        _uncommitedDomainEvents.Add(@event);
        Mutate(@event);
    }

    private void Mutate(IDomainEvent @event)
    {
        ((dynamic)this).On((dynamic)@event);
    }
}

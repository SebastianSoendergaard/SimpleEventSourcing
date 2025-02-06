using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.Projections;

namespace Basses.SimpleEventStore.Enablers;

public abstract class EventSourcedRepository<T> where T : Aggregate
{
    private readonly IEventStore _eventStore;
    private readonly ProjectionManager _projectionManager;

    public EventSourcedRepository(IEventStore eventStore, ProjectionManager projectionManager)
    {
        _eventStore = eventStore;
        _projectionManager = projectionManager;
    }

    public async Task Add(T aggregate)
    {
        var events = aggregate.UncommitedDomainEvents;
        await _eventStore.AppendEvents(aggregate.Id, aggregate.Version + 1, events);
        aggregate.ClearDomainEvents();
    }

    public async Task Update(T aggregate)
    {
        var events = aggregate.UncommitedDomainEvents;
        await _eventStore.AppendEvents(aggregate.Id, aggregate.Version + 1, events);
        aggregate.ClearDomainEvents();
    }

    public async Task<T> Get(Guid aggregateId)
    {
        var eventEntries = await _eventStore.LoadEvents(aggregateId);

        var events = eventEntries
            .Select(e => e.Event as IDomainEvent)
            .Where(e => e != null)
            .Select(e => e!);

        var aggregate = Activator.CreateInstance(typeof(T), events);

        return (aggregate as T) ?? throw new InvalidOperationException($"Can not create aggregate of type: {typeof(T).FullName}");
    }
}

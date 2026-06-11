using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace Framework;

public class EventSourcedPiiRepository<TAggregate, TPiiData>
    : EventSourcedRepository<TAggregate> where TAggregate : Aggregate
{


    public EventSourcedPiiRepository(IEventStore eventStore) : base(eventStore)
    {
    }

    public new async Task Add(TAggregate aggregate)
    {
        //var events = aggregate.UncommitedDomainEvents;
        //await _eventStore.AppendEvents(aggregate.Id, 1, events);
        //aggregate.ClearDomainEvents();

        await base.Add(aggregate);

    }

    public new async Task Update(TAggregate aggregate)
    {
        await base.Update(aggregate);
    }

    public new async Task<TAggregate?> TryGet(string aggregateId)
    {
        return await base.TryGet(aggregateId);
    }

    private class PiiDataWrapper<TPiiData>
    {
        public required TPiiData Data { get; set; }
        public int Version { get; set; }
    }
}



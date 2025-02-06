using EventSourcing.EventStore;
using EventSourcing.Projections;

namespace EventSourcing.Enablers;

public abstract class Projector : IProjector
{
    public abstract Guid Id { get; }

    public string Name => GetType().Name;

    private long _sequenceNumber;

    protected abstract Task<long> GetSequenceNumber();

    public async Task Update(IEnumerable<EventEntry> events)
    {
        await UpdateStarting();

        foreach (var @event in events)
        {
            var domainEvent = @event.Event as IDomainEvent ?? throw new InvalidOperationException($"EventStore event cannot be converted to domain event: {@event.Event.GetType().FullName}");
            var eventData = new EventData(@event.StreamId, @event.Version, @event.Timestamp);
            await Mutate(domainEvent, eventData);
            _sequenceNumber = @event.SequenceNumber;
        }

        await UpdateComplete(_sequenceNumber);
    }

    private Task Mutate(IDomainEvent @event, EventData eventData)
    {
        if (MutationRegistry.CanMutate(this, @event, typeof(IProjectionEventHandler<>)))
        {
            return ((dynamic)this).UpdateWith((dynamic)@event, eventData);
        }
        return Task.CompletedTask;
    }

    public async Task<long> LoadSequenceNumber()
    {
        _sequenceNumber = await GetSequenceNumber();
        return _sequenceNumber;
    }

    protected virtual Task UpdateStarting() { return Task.CompletedTask; }
    protected virtual Task UpdateComplete(long sequenceNumber) { return Task.CompletedTask; }
}

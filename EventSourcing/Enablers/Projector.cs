using EventSourcing.EventStore;
using EventSourcing.Projections;

namespace EventSourcing.Enablers;

public abstract class Projector : IProjector
{
    public Guid Id => GetId();

    public string Name => GetType().Name;

    private ISet<string>? _eventTypes;
    private long _sequenceNumber;

    protected abstract Guid GetId();
    protected abstract IEnumerable<Type> GetDomainEventTypes();
    protected abstract Task<long> GetSequenceNumber();

    public async Task Update(IEnumerable<EventEntry> events)
    {
        _eventTypes ??= GetDomainEventTypes().Select(x => x.AssemblyQualifiedName ?? "").ToHashSet();

        await UpdateStarting();

        foreach (var @event in events)
        {
            if (_eventTypes.Contains(@event.EventType))
            {
                var domainEvent = @event.Event as IDomainEvent ?? throw new InvalidOperationException($"EventStore event cannot be converted to domain event: {@event.Event.GetType().FullName}");
                await Mutate(@event.StreamId, @event.Version, @event.Timestamp, domainEvent);
            }

            _sequenceNumber = @event.SequenceNumber;
        }

        await UpdateComplete(_sequenceNumber);
    }

    private Task Mutate(Guid streamId, int version, DateTimeOffset timestamp, IDomainEvent @event)
    {
        return ((dynamic)this).UpdateWith((dynamic)@event, streamId, version, timestamp);
    }

    public async Task<long> LoadSequenceNumber()
    {
        _sequenceNumber = await GetSequenceNumber();
        return _sequenceNumber;
    }

    protected virtual Task UpdateStarting() { return Task.CompletedTask; }
    protected virtual Task UpdateComplete(long sequenceNumber) { return Task.CompletedTask; }
}

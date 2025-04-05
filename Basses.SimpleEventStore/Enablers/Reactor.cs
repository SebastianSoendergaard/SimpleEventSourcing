using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;
using Basses.SimpleEventStore.Reactions;

namespace Basses.SimpleEventStore.Enablers;

public abstract class Reactor : IReactor
{
    private long _sequenceNumber = 0;

    public virtual string Name => GetType().Name;

    public async Task Update(IEnumerable<EventEntry> events)
    {
        await UpdateStarting();

        foreach (var @event in events)
        {
            var domainEvent = @event.Event as IDomainEvent ?? throw new InvalidOperationException($"EventStore event cannot be converted to domain event: {@event.Event.GetType().FullName}");
            var eventData = new EventData(@event.SequenceNumber, @event.StreamId, @event.Version, @event.Timestamp);
            await Mutate(domainEvent, eventData);
            _sequenceNumber = @event.SequenceNumber;
        }

        await UpdateComplete(_sequenceNumber);
    }

    private Task Mutate(IDomainEvent @event, EventData eventData)
    {
        if (MutationRegistry.CanMutate(this, @event, typeof(IReactionEventHandler<>)))
        {
            return ((dynamic)this).ReactOn((dynamic)@event, eventData);
        }
        return Task.CompletedTask;
    }

    public async Task<long> GetSequenceNumber(EventSubscriberProcessingState currentState)
    {
        if (_sequenceNumber == 0)
        {
            _sequenceNumber = currentState.ConfirmedSequenceNumber;
        }
        _sequenceNumber = await LoadSequenceNumber();
        return _sequenceNumber;
    }

    protected virtual Task<long> LoadSequenceNumber() { return Task.FromResult(_sequenceNumber); }
    protected virtual Task UpdateStarting() { return Task.CompletedTask; }
    protected virtual Task UpdateComplete(long sequenceNumber) { return Task.CompletedTask; }
}

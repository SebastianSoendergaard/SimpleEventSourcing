namespace Basses.SimpleEventStore.EventStore.InMemory;

public class InMemoryEventStore : IEventStore
{
    private readonly List<EventEntry> _events = [];
    private readonly object _lock = new();
    private readonly IEventSerializer _eventSerializer;
    private readonly UpcastManager _upcastManager;
    private Func<Task>? _onEventsAppended;

    public InMemoryEventStore()
    {
        _eventSerializer = new DefaultEventSerializer();
        _upcastManager = new UpcastManager(_eventSerializer);
    }

    public async Task AppendEvents(string streamId, int version, IEnumerable<object> events)
    {
        lock (_lock)
        {
            var sequenceNumber = _events.Count + 1;

            foreach (var @event in events)
            {
                if (_events.Any(e => e.StreamId == streamId && e.Version == version))
                {
                    throw new VersionConflictException($"Stream '{streamId}' with version '{version}' already exists");
                }

                EventEntry eventEntry = new(
                    sequenceNumber++,
                    streamId,
                    version++,
                    DateTimeOffset.UtcNow,
                    @event.GetType().AssemblyQualifiedName ?? "",
                    @event
                );

                _events.Add(eventEntry);
            }
        }

        if (_onEventsAppended != null)
        {
            await _onEventsAppended.Invoke();
        }
    }

    public Task<long> GetHeadSequenceNumber()
    {
        return Task.FromResult((long)_events.Count);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(string streamId)
    {
        var events = GetEvents()
            .Where(e => e.StreamId == streamId);

        return Task.FromResult(events);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(string streamId, long startSequenceNumber, int max)
    {
        var events = GetEvents()
            .Where(e => e.SequenceNumber >= startSequenceNumber)
            .Where(e => e.StreamId == streamId)
            .Take(max);

        return Task.FromResult(events);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(long startSequenceNumber, int max)
    {
        var events = GetEvents()
            .Where(e => e.SequenceNumber >= startSequenceNumber)
            .Take(max);

        return Task.FromResult(events);
    }

    private IEnumerable<EventEntry> GetEvents()
    {
        foreach (var @event in _events)
        {
            // We take the penalty of serializing to be able to use the standard upcasting mechanisms
            var serializedEvent = _eventSerializer.Serialize(@event.Event);
            var upcastedEvent = _upcastManager.Deserialize(serializedEvent.EventPayload, @event.EventType);
            yield return @event with { EventType = upcastedEvent.GetType().AssemblyQualifiedName ?? "", Event = upcastedEvent };
        }
    }

    public void RegisterUpcaster(IUpcaster upcaster)
    {
        _upcastManager.RegisterUpcaster(upcaster);
    }

    public void RegisterForEventsAppendedNotifications(Func<Task> onEventsAppended)
    {
        _onEventsAppended += onEventsAppended;
    }
}

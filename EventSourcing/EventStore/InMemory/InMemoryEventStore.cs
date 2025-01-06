namespace EventSourcing.EventStore.InMemory;

public class InMemoryEventStore : IEventStore
{
    private readonly List<EventEntry> _events = [];
    private readonly object _lock = new();
    private Func<Task>? _onEventsAppended;

    public async Task AppendEvents(Guid streamId, int version, IEnumerable<object> events)
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

    public Task<IEnumerable<EventEntry>> LoadEvents(Guid streamId)
    {
        var events = _events
            .Where(e => e.StreamId == streamId);

        return Task.FromResult(events);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(Guid streamId, long startSequenceNumber, int max)
    {
        var events = _events
            .Where(e => e.SequenceNumber >= startSequenceNumber)
            .Where(e => e.StreamId == streamId)
            .Take(max);

        return Task.FromResult(events);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(long startSequenceNumber, int max)
    {
        var events = _events
            .Where(e => e.SequenceNumber >= startSequenceNumber)
            .Take(max);

        return Task.FromResult(events);
    }

    public void RegisterForEventsAppendedNotifications(Func<Task> onEventsAppended)
    {
        _onEventsAppended += onEventsAppended;
    }
}

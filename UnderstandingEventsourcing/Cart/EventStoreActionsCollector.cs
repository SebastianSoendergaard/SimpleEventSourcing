using System.Diagnostics;
using Basses.SimpleEventStore;

namespace Cart;

internal class EventStoreActionsCollector : IInstrumentation
{
    private object _lock = new object();
    private Dictionary<Guid, EventStoreAction> _actions = new();

    public void StartingAction(string type, Guid id, object? details = null)
    {
        lock (_lock)
        {
            _actions.Add(id, new EventStoreAction(type, id, details));
        }
    }

    public void CompletedAction(string type, Guid id, object? details = null)
    {
        lock (_lock)
        {
            if (_actions.TryGetValue(id, out var action))
            {
                action.Finish(details);
            }
        }
    }

    public IEnumerable<EventStoreAction> CollectActions()
    {
        Dictionary<Guid, EventStoreAction> actions;
        lock (_lock)
        {
            actions = _actions;
            _actions = new();
        }
        return actions.Values;
    }
}

internal class EventStoreAction
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public string? Type { get; }
    public Guid Id { get; }
    public object? StartDetails { get; }
    public object? CompleteDetails { get; private set; }
    public TimeSpan ProcessingTime => _stopwatch.Elapsed;

    public EventStoreAction(string type, Guid id, object? details = null)
    {
        Type = type;
        Id = id;
        StartDetails = details;
    }

    public void Finish(object? details = null)
    {
        CompleteDetails = details;
        _stopwatch.Stop();
    }
}

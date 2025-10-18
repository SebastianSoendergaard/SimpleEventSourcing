using System.Diagnostics;
using Basses.SimpleEventStore;

namespace Cart;

internal class EventStoreInstrumentation : IInstrumentation
{
    Dictionary<Guid, EventStoreAction> _actions = new();

    public void StartingAction(string type, Guid id, object? details = null)
    {
        _actions.Add(id, new EventStoreAction(type, id, details));
    }

    public void FinishedAction(string type, Guid id, object? details = null)
    {
        if (_actions.TryGetValue(id, out var action))
        {
            action.Finish(details);
        }
    }

    public IEnumerable<EventStoreAction> GetAllActions()
    {
        return _actions.Values.ToList();
    }
}

internal class EventStoreAction
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public string? Type { get; }
    public Guid Id { get; }
    public object? StartDetails { get; }
    public object? FinishDetails { get; private set; }
    public TimeSpan ExecutionTime => _stopwatch.Elapsed;

    public EventStoreAction(string type, Guid id, object? details = null)
    {
        Type = type;
        Id = id;
        StartDetails = details;
    }

    public void Finish(object? details = null)
    {
        FinishDetails = details;
        _stopwatch.Stop();
    }
}

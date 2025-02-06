using Basses.SimpleEventStore.EventStore;

namespace Basses.SimpleEventStore.Projections;

public class ProjectionManager
{
    private readonly IEventStore _eventStore;
    private readonly IProjectorStateStore _projectorStateStore;
    private readonly ExecutionLoop _asyncLoop = new();
    private readonly List<IProjector> _synchronousProjectors = [];
    private readonly List<IProjector> _asynchronousProjectors = [];

    public ProjectionManager(IEventStore eventStore, IProjectorStateStore projectorStateStore)
    {
        _eventStore = eventStore;
        _projectorStateStore = projectorStateStore;

        _eventStore.RegisterForEventsAppendedNotifications(UpdateSynchronousProjections);
    }

    public void RegisterSynchronousProjector(IProjector projector)
    {
        _projectorStateStore.UpsertProjector(projector);
        _synchronousProjectors.Add(projector);
    }

    public void RegisterAsynchronousProjector(IProjector projector)
    {
        _projectorStateStore.UpsertProjector(projector);
        _asynchronousProjectors.Add(projector);
    }

    public void Start()
    {
        _asyncLoop.Start(
            TimeSpan.FromSeconds(20),
            (cancellationToken) => UpdateAsynchronousProjections(),
            exception => { }
        );
    }

    public void Stop()
    {
        _asyncLoop.Stop();
    }

    private Task UpdateSynchronousProjections()
    {
        return UpdateProjections(_synchronousProjectors);
    }

    private Task UpdateAsynchronousProjections()
    {
        return UpdateProjections(_asynchronousProjectors);

        // TODO: continue updates if not done yet
    }

    private async Task UpdateProjections(IEnumerable<IProjector> projectors)
    {
        var headSequenceNumber = await _eventStore.GetHeadSequenceNumber();
        var eventCache = new Dictionary<long, IEnumerable<EventEntry>>();

        foreach (var projector in projectors)
        {
            var currentState = await _projectorStateStore.GetProcessingState(projector);

            try
            {
                var events = await LoadEvents(projector, headSequenceNumber, eventCache);
                if (events.Any())
                {
                    await ApplyEvents(projector, currentState, events);

                    var newState = new ProjectorProcessingState(DateTimeOffset.UtcNow);
                    await _projectorStateStore.SaveProcessingState(projector, newState);
                }
            }
            catch (Exception ex)
            {
                var error = new ProjectorProcessingError(ex.Message, ex.StackTrace ?? "", currentState.ProcessingError?.ProcessingAttempts ?? 1, DateTimeOffset.Now);
                var newState = new ProjectorProcessingState(currentState.LatestSuccessfulProcessingTime, error);
                await _projectorStateStore.SaveProcessingState(projector, newState);
            }
        }
    }

    private async Task<IEnumerable<EventEntry>> LoadEvents(IProjector projector, long headSequenceNumber, Dictionary<long, IEnumerable<EventEntry>> eventCache)
    {
        var sequenceNumber = await projector.LoadSequenceNumber();

        if (eventCache.ContainsKey(sequenceNumber))
        {
            return eventCache[sequenceNumber];
        }

        if (sequenceNumber < headSequenceNumber)
        {
            const int max = 50; // We need a limit to ensure that one projector does not block them all
            var events = await _eventStore.LoadEvents(sequenceNumber + 1, max);
            eventCache.Add(sequenceNumber, events);
            return events;
        }

        return [];
    }

    private async Task ApplyEvents(IProjector projector, ProjectorProcessingState currentState, IEnumerable<EventEntry> events)
    {
        if (currentState.ProcessingError != null)
        {
            // Process one event at the time until we reach the failing one
            foreach (var @event in events)
            {
                await projector.Update(new[] { @event });
            }
        }
        else
        {
            // We expect every thing to be ok, so lets try to process the entire batch
            await projector.Update(events);
        }
    }

    public IEnumerable<IProjector> GetProjectors()
    {
        return _synchronousProjectors.Concat(_asynchronousProjectors);
    }

    public Task<ProjectorProcessingState> GetProcessingState(Guid projectorId)
    {
        var projector = GetProjectors().Single(x => x.Id == projectorId);

        return _projectorStateStore.GetProcessingState(projector);
    }

    public T GetProjector<T>(Guid projectorId) where T : class, IProjector
    {
        var projector = _synchronousProjectors
            .Concat(_asynchronousProjectors)
            .SingleOrDefault(x => x.Id == projectorId);

        var typedProjector = projector as T;

        return typedProjector ?? throw new InvalidOperationException($"No projector with id '{projectorId}' can be found");
    }
}

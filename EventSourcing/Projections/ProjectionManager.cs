using Basses.SimpleEventStore.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace Basses.SimpleEventStore.Projections;

public class ProjectionManager
{
    private readonly IEventStore _eventStore;
    private readonly IProjectorStateStore _projectorStateStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly ExecutionLoop _asyncLoop = new();
    private readonly List<Type> _synchronousProjectors = [];
    private readonly List<Type> _asynchronousProjectors = [];

    public ProjectionManager(IEventStore eventStore, IProjectorStateStore projectorStateStore, IServiceProvider serviceProvider)
    {
        _eventStore = eventStore;
        _projectorStateStore = projectorStateStore;
        _serviceProvider = serviceProvider;
        _eventStore.RegisterForEventsAppendedNotifications(UpdateSynchronousProjections);
    }

    public ProjectionManager RegisterSynchronousProjector<TProjector>() where TProjector : IProjector
    {
        using var scope = _serviceProvider.CreateScope();
        var projector = scope.ServiceProvider.GetRequiredService<TProjector>();
        _projectorStateStore.UpsertProjector(projector);
        _synchronousProjectors.Add(projector.GetType());
        return this;
    }

    public ProjectionManager RegisterAsynchronousProjector<TProjector>() where TProjector : IProjector
    {
        using var scope = _serviceProvider.CreateScope();
        var projector = scope.ServiceProvider.GetRequiredService<TProjector>();
        _projectorStateStore.UpsertProjector(projector);
        _synchronousProjectors.Add(projector.GetType());
        return this;
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

    private async Task UpdateProjections(IEnumerable<Type> projectorTypes)
    {
        using var scope = _serviceProvider.CreateScope();

        var headSequenceNumber = await _eventStore.GetHeadSequenceNumber();
        var eventCache = new Dictionary<long, IEnumerable<EventEntry>>();

        foreach (var projectorType in projectorTypes)
        {
            var projector = (IProjector)scope.ServiceProvider.GetRequiredService(projectorType);

            var currentState = await _projectorStateStore.GetProcessingState(projector);

            try
            {
                var events = await LoadEvents(projector, headSequenceNumber, eventCache);
                if (events.Any())
                {
                    await ApplyEvents(projector, currentState, events);

                    var newState = new ProjectorProcessingState(DateTimeOffset.UtcNow, events.Last().SequenceNumber);
                    await _projectorStateStore.SaveProcessingState(projector, newState);
                }
            }
            catch (Exception ex)
            {
                var error = new ProjectorProcessingError(ex.Message, ex.StackTrace ?? "", currentState.ProcessingError?.ProcessingAttempts ?? 1, DateTimeOffset.UtcNow);
                var newState = new ProjectorProcessingState(currentState.LatestSuccessfulProcessingTime, currentState.ConfirmedSequenceNumber, error);
                await _projectorStateStore.SaveProcessingState(projector, newState);
            }
        }
    }

    private async Task<IEnumerable<EventEntry>> LoadEvents(IProjector projector, long headSequenceNumber, Dictionary<long, IEnumerable<EventEntry>> eventCache)
    {
        var sequenceNumber = await projector.GetSequenceNumber();

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

    public IEnumerable<Type> GetProjectorTypes()
    {
        return _synchronousProjectors.Concat(_asynchronousProjectors);
    }

    public Task<ProjectorProcessingState> GetProcessingState(IProjector projector)
    {
        return _projectorStateStore.GetProcessingState(projector);
    }
}

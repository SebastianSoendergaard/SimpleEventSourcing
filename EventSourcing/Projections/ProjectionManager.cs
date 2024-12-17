using EventSourcing.EventStore;

namespace EventSourcing.Projections;

public class ProjectionManager
{
    private readonly IEventStore _eventStore;
    private readonly List<IProjector> _liveProjectors = [];
    private readonly List<IProjector> _eventualProjectors = [];

    public ProjectionManager(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public void RegisterLiveProjector(IProjector projector)
    {
        _liveProjectors.Add(projector);

        // TODO: Save projector in database, if it does not exist
    }

    public void RegisterEventualProjector(IProjector projector)
    {
        _eventualProjectors.Add(projector);

        // TODO: Save projector in database, if it does not exist
    }

    public Task UpdateLiveProjections()
    {
        return UpdateProjections(_liveProjectors);
    }

    public Task UpdateEventualProjections()
    {
        return UpdateProjections(_eventualProjectors);
    }

    private async Task UpdateProjections(IEnumerable<IProjector> projectors)
    {
        var headSequenceNumber = await _eventStore.GetHeadSequenceNumber();

        foreach (var projector in projectors)
        {
            if (projector.SequenceNumber < headSequenceNumber)
            {
                const int max = 50; // We need a limit to ensure that one projector does not block them all
                var events = await _eventStore.LoadEvents(projector.SequenceNumber + 1, max);
                await projector.Update(events);

                // TODO: Save projector state in database
            }
        }
    }

    public T GetProjector<T>(Guid projectorId) where T : class, IProjector
    {
        var projector = _liveProjectors
            .Concat(_eventualProjectors)
            .SingleOrDefault(x => x.Id == projectorId);

        var typedProjector = projector as T;

        return typedProjector ?? throw new InvalidOperationException($"No projector with id '{projectorId}' can be found");
    }
}

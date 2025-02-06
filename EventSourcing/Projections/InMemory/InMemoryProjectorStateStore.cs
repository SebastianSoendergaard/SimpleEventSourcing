namespace Basses.SimpleEventStore.Projections.InMemory;

public class InMemoryProjectorStateStore : IProjectorStateStore
{
    private readonly Dictionary<Guid, ProjectorProcessingState> _processingStates = [];

    public Task<ProjectorProcessingState> GetProcessingState(IProjector projector)
    {
        return Task.FromResult(_processingStates[projector.Id]);
    }

    public Task SaveProcessingState(IProjector projector, ProjectorProcessingState state)
    {
        _processingStates[projector.Id] = state;
        return Task.CompletedTask;
    }

    public Task UpsertProjector(IProjector projector)
    {
        if (!_processingStates.ContainsKey(projector.Id))
        {
            _processingStates.Add(projector.Id, new ProjectorProcessingState(DateTimeOffset.MinValue));
        }
        return Task.CompletedTask;
    }
}

namespace Basses.SimpleEventStore.Projections.InMemory;

public class InMemoryProjectorStateStore : IProjectorStateStore
{
    private readonly Dictionary<string, ProjectorProcessingState> _processingStates = [];

    public Task<ProjectorProcessingState> GetProcessingState(IProjector projector)
    {
        return Task.FromResult(_processingStates[projector.Name]);
    }

    public Task SaveProcessingState(IProjector projector, ProjectorProcessingState state)
    {
        _processingStates[projector.Name] = state;
        return Task.CompletedTask;
    }

    public Task UpsertProjector(IProjector projector)
    {
        if (!_processingStates.ContainsKey(projector.Name))
        {
            _processingStates.Add(projector.Name, new ProjectorProcessingState(DateTimeOffset.MinValue, 0));
        }
        return Task.CompletedTask;
    }
}

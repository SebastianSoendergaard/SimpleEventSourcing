namespace EventSourcing.Projections.Files;

public class FileProjectorStateStore : IProjectorStateStore
{
    public Task<ProjectorProcessingState> GetProcessingState(IProjector projector)
    {
        throw new NotImplementedException();
    }

    public Task SaveProcessingState(IProjector projector, ProjectorProcessingState state)
    {
        throw new NotImplementedException();
    }

    public Task UpsertProjector(IProjector projector)
    {
        throw new NotImplementedException();
    }
}

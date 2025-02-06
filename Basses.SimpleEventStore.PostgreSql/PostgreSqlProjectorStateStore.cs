using Basses.SimpleEventStore.Projections;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlProjectorStateStore : IProjectorStateStore
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

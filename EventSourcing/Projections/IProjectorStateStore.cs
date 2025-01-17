namespace EventSourcing.Projections;

public interface IProjectorStateStore
{
    Task UpsertProjector(IProjector projector);
    Task SaveProcessingState(IProjector projector, ProjectorProcessingState state);
    Task<ProjectorProcessingState> GetProcessingState(IProjector projector);
}

public record ProjectorProcessingState(DateTimeOffset LatestSuccessfulProcessingTime, ProjectorProcessingError? ProcessingError = null);
public record ProjectorProcessingError(string ErrorMessage, string Stacktrace, int ProcessingAttempts, DateTimeOffset LatestRetryTime);


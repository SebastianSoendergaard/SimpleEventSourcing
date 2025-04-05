namespace Basses.SimpleEventStore.EventSubscriber;

public interface IEventSubscriberStateStore
{
    Task UpsertSubscriber(IEventSubscriber subscriber);
    Task SaveProcessingState(IEventSubscriber subscriber, EventSubscriberProcessingState state);
    Task<EventSubscriberProcessingState> GetProcessingState(IEventSubscriber subscriber);
}

public record EventSubscriberProcessingState(
    DateTimeOffset LatestSuccessfulProcessingTime,
    long ConfirmedSequenceNumber,
    EventSubscriberProcessingError? ProcessingError = null
);

public record EventSubscriberProcessingError(
    string ErrorMessage,
    string Stacktrace,
    int ProcessingAttempts,
    DateTimeOffset LatestRetryTime
);


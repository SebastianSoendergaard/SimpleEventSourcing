namespace Basses.SimpleEventStore.EventSubscriber.InMemory;

public abstract class InMemoryEventSubscriberStateStore : IEventSubscriberStateStore
{
    private readonly Dictionary<string, EventSubscriberProcessingState> _processingStates = [];

    public Task<EventSubscriberProcessingState> GetProcessingState(IEventSubscriber projector)
    {
        return Task.FromResult(_processingStates[projector.Name]);
    }

    public Task SaveProcessingState(IEventSubscriber projector, EventSubscriberProcessingState state)
    {
        _processingStates[projector.Name] = state;
        return Task.CompletedTask;
    }

    public Task UpsertSubscriber(IEventSubscriber projector)
    {
        if (!_processingStates.ContainsKey(projector.Name))
        {
            _processingStates.Add(projector.Name, new EventSubscriberProcessingState(DateTimeOffset.MinValue, 0));
        }
        return Task.CompletedTask;
    }
}

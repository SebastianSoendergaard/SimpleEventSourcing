using Basses.SimpleEventStore.EventStore;

namespace Basses.SimpleEventStore.EventSubscriber;

public interface IEventSubscriber
{
    string Name { get; }
    Task<long> GetSequenceNumber(EventSubscriberProcessingState currentState);
    Task Update(IEnumerable<EventEntry> events);
}

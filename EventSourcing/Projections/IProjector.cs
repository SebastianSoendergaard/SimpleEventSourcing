using Basses.SimpleEventStore.EventStore;

namespace Basses.SimpleEventStore.Projections;

public interface IProjector
{
    string Name { get; }
    Task<long> GetSequenceNumber();
    Task Update(IEnumerable<EventEntry> events);
}

using Basses.SimpleEventStore.EventStore;

namespace Basses.SimpleEventStore.Projections;

public interface IProjector
{
    Guid Id { get; }
    string Name { get; }
    Task<long> LoadSequenceNumber();
    Task Update(IEnumerable<EventEntry> events);
}

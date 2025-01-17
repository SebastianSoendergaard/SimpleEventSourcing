using EventSourcing.EventStore;

namespace EventSourcing.Projections;

public interface IProjector
{
    Guid Id { get; }
    string Name { get; }
    Task<long> LoadSequenceNumber();
    Task Update(IEnumerable<EventEntry> events);
}

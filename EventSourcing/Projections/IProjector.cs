using EventSourcing.EventStore;

namespace EventSourcing.Projections
{
    public interface IProjector
    {
        Guid Id { get; }
        string Name { get; }
        long SequenceNumber { get; }

        Task Update(IEnumerable<EventEntry> events);
    }
}

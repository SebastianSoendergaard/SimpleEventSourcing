namespace EventSourcing.EventStore
{
    public interface IEventStore
    {
        Task AppendEvents(Guid streamId, int version, IEnumerable<object> events);
        Task<long> GetHeadSequenceNumber();
        Task<IEnumerable<EventEntry>> LoadEvents(Guid streamId);
        Task<IEnumerable<EventEntry>> LoadEvents(Guid streamId, long startSequenceNumber, int max);
        Task<IEnumerable<EventEntry>> LoadEvents(long startSequenceNumber, int max);
    }
}

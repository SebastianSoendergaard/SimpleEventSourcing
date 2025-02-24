namespace Basses.SimpleEventStore.EventStore
{
    public interface IEventStore
    {
        Task AppendEvents(string streamId, int version, IEnumerable<object> events);
        Task<long> GetHeadSequenceNumber();
        Task<IEnumerable<EventEntry>> LoadEvents(string streamId);
        Task<IEnumerable<EventEntry>> LoadEvents(string streamId, long startSequenceNumber, int max);
        Task<IEnumerable<EventEntry>> LoadEvents(long startSequenceNumber, int max);
        void RegisterUpcaster(IUpcaster upcaster);
        void RegisterForEventsAppendedNotifications(Func<Task> onEventsAppended);
    }
}

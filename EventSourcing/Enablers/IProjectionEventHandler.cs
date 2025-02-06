namespace Basses.SimpleEventStore.Enablers
{
    public interface IProjectionEventHandler<T> where T : IDomainEvent
    {
        Task UpdateWith(T @event, EventData eventData);
    }

    public record EventData(Guid StreamId, int Version, DateTimeOffset Timestamp);
}

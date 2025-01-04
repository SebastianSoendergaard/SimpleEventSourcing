using EventSourcing.EventStore;
using EventSourcing.Projections;

namespace WebApi.Domain
{
    public abstract class Projector : IProjector
    {
        public Guid Id => GetId();

        public string Name => GetType().Name;

        public long SequenceNumber { get; private set; }

        private ISet<string>? _eventTypes;

        protected abstract Guid GetId();
        protected abstract IEnumerable<Type> GetDomainEventTypes();

        public async Task Update(IEnumerable<EventEntry> events)
        {
            _eventTypes ??= GetDomainEventTypes().Select(x => x.AssemblyQualifiedName ?? "").ToHashSet();

            foreach (var @event in events)
            {
                if (_eventTypes.Contains(@event.EventType))
                {
                    var domainEvent = @event.Event as IDomainEvent ?? throw new InvalidOperationException($"EventStore event cannot be converted to domain event: {@event.Event.GetType().FullName}");
                    await Mutate(@event.StreamId, @event.Version, @event.Timestamp, domainEvent);
                }

                SequenceNumber = @event.SequenceNumber;
            }
        }

        private Task Mutate(Guid streamId, int version, DateTimeOffset timestamp, IDomainEvent @event)
        {
            return ((dynamic)this).UpdateWith((dynamic)@event, streamId, version, timestamp);
        }
    }
}

using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Tests;

public class QueryValidator
{
    private readonly IEventStore _eventStore;
    private readonly Guid _streamId;
    private readonly Projector? _projector;
    private int _version = 0;

    private QueryValidator(IEventStore eventStore, Guid streamId, Projector? projector = null)
    {
        _eventStore = eventStore;
        _streamId = streamId;
        _projector = projector;
    }

    public static QueryValidator Setup(IEventStore eventStore, Guid streamId)
    {
        return new QueryValidator(eventStore, streamId);
    }

    public QueryValidator Given(params IDomainEvent[] givenEvents)
    {
        return Given(givenEvents);
    }

    public QueryValidator Given(IEnumerable<IDomainEvent> givenEvents)
    {
        _eventStore.AppendEvents(_streamId, _version, givenEvents);
        _version += givenEvents.Count();
        return this;
    }

    public async Task Then<TReadModel>(Func<Task<object>> queryExecution, Action<TReadModel> onReadModel) where TReadModel : class
    {
        if (queryExecution == null)
        {
            throw new Exception("No When statement given");
        }

        if (_projector != null)
        {
            var events = await _eventStore.LoadEvents(0, int.MaxValue);
            await _projector.Update(events);
        }

        var readModel = await queryExecution.Invoke();
        onReadModel((TReadModel)readModel);
    }

    public async Task Then<TException>(Func<Task<object>> queryExecution) where TException : Exception
    {
        if (queryExecution == null)
        {
            throw new Exception("No When statement given");
        }

        await Assert.ThrowsAsync<TException>(queryExecution.Invoke);
    }
}

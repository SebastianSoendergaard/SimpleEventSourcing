using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Tests;

public class CommandValidator
{
    private readonly IEventStore _eventStore;
    private readonly Guid _streamId;
    private int _version = 0;

    private Func<Task>? _commandExecution;

    private CommandValidator(IEventStore eventStore, Guid streamId)
    {
        _eventStore = eventStore;
        _streamId = streamId;
    }

    public static CommandValidator Setup(IEventStore eventStore, Guid streamId)
    {
        return new CommandValidator(eventStore, streamId);
    }

    public CommandValidator Given(params IDomainEvent[] givenEvents)
    {
        return Given(givenEvents);
    }

    public CommandValidator Given(IEnumerable<IDomainEvent> givenEvents)
    {
        _eventStore.AppendEvents(_streamId, _version, givenEvents);
        _version += givenEvents.Count();
        return this;
    }

    public CommandValidator When(Func<Task> commandExecution)
    {
        _commandExecution = commandExecution;
        return this;
    }

    public Task Then(params IDomainEvent[] expectedEvents)
    {
        return Then(expectedEvents);
    }

    public async Task Then(IEnumerable<IDomainEvent> expectedEvents)
    {
        _commandExecution?.Invoke();
        var actualEvents = await _eventStore.LoadEvents(_streamId, _version + 1, int.MaxValue);

        foreach (var expected in expectedEvents.Select((Value, Index) => (Value, Index)))
        {
            var index = expected.Index;
            var expectedEvent = expected.Value;

            if (actualEvents.Count() < index)
            {
                throw new Exception($"Expected event not found: {expectedEvent}");
            }

            var actualEvent = actualEvents.ToList()[index].Event;
            Assert.Equal(expectedEvent, actualEvent);
        }
    }

    public async Task Then<TException>() where TException : Exception
    {
        if (_commandExecution == null)
        {
            throw new Exception("No When statement given");
        }

        await Assert.ThrowsAsync<TException>(_commandExecution.Invoke);
    }
}

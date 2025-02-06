using Basses.SimpleEventStore.EventStore;

namespace EventSourcing.Test;

public sealed class EventStoreTests
    : IClassFixture<InMemoryStoreFixture>, IClassFixture<FileStoreFixture>, IClassFixture<PostgreSqlStoreFixture>
{
    private static InMemoryStoreFixture? _inMemoryStoreFixture;
    private static FileStoreFixture? _fileStoreFixture;
    private static PostgreSqlStoreFixture? _postgreSqlStoreFixture;

    public EventStoreTests(InMemoryStoreFixture inMemoryStoreFixture, FileStoreFixture fileStoreFixture, PostgreSqlStoreFixture postgreSqlStoreFixture)
    {
        _inMemoryStoreFixture = inMemoryStoreFixture;
        _fileStoreFixture = fileStoreFixture;
        _postgreSqlStoreFixture = postgreSqlStoreFixture;
    }

    [Theory]
    [MemberData(nameof(EventStoreFactory))]
    public async Task CanAppendEventAndLoadEvent(Func<IEventStore> eventStoreFactory)
    {
        var eventStore = eventStoreFactory();

        var streamId = Guid.NewGuid();
        var @event = new CreatedEvent(1, "abc");

        await eventStore.AppendEvents(streamId, 1, new[] { @event });

        var events = await eventStore.LoadEvents(0, int.MaxValue);

        Assert.Single(events);
    }

    [Theory]
    [MemberData(nameof(EventStoreFactory))]
    public async Task CanAppendAndLoadMultipleEvents(Func<IEventStore> eventStoreFactory)
    {
        var eventStore = eventStoreFactory();

        var streamId = Guid.NewGuid();
        var event1 = new CreatedEvent(1, "abc");
        var event2 = new UpdatedEvent(1, "cba");
        var event3 = new RemovedEvent(1);

        await eventStore.AppendEvents(streamId, 1, new object[] { event1, event2, event3 });

        var events = await eventStore.LoadEvents(0, int.MaxValue);

        Assert.Equal(3, events.Count());
    }

    [Theory]
    [MemberData(nameof(EventStoreFactory))]
    public async Task CanAppendEventsToMultipleStreams(Func<IEventStore> eventStoreFactory)
    {
        var eventStore = eventStoreFactory();

        var streamId1 = Guid.NewGuid();
        var event1 = new CreatedEvent(1, "abc");
        await eventStore.AppendEvents(streamId1, 1, new object[] { event1 });

        var streamId2 = Guid.NewGuid();
        var event2 = new CreatedEvent(2, "abc");
        await eventStore.AppendEvents(streamId2, 1, new object[] { event2 });

        var streamId3 = Guid.NewGuid();
        var event3 = new CreatedEvent(3, "abc");
        await eventStore.AppendEvents(streamId3, 1, new object[] { event3 });

        var events = await eventStore.LoadEvents(0, int.MaxValue);

        Assert.Equal(3, events.Count());
    }

    [Theory]
    [MemberData(nameof(EventStoreFactory))]
    public async Task CanReturnHeadSequenceNumber(Func<IEventStore> eventStoreFactory)
    {
        var eventStore = eventStoreFactory();

        var streamId = Guid.NewGuid();
        var event1 = new CreatedEvent(1, "abc");
        var event2 = new UpdatedEvent(1, "cba");
        var event3 = new RemovedEvent(1);

        await eventStore.AppendEvents(streamId, 1, new[] { event1 });
        var head = await eventStore.GetHeadSequenceNumber();
        Assert.Equal(1, head);

        await eventStore.AppendEvents(streamId, 2, new[] { event2 });
        head = await eventStore.GetHeadSequenceNumber();
        Assert.Equal(2, head);

        await eventStore.AppendEvents(streamId, 3, new[] { event3 });
        head = await eventStore.GetHeadSequenceNumber();
        Assert.Equal(3, head);
    }

    private record CreatedEvent(int Id, string Text);
    private record UpdatedEvent(int Id, string NewText);
    private record RemovedEvent(int Id);

    public static IEnumerable<object[]> EventStoreFactory
    {
        get
        {
            yield return new object[] { new Func<IEventStore>(() => _inMemoryStoreFixture!.CreateEventStore()) };
            yield return new object[] { new Func<IEventStore>(() => _fileStoreFixture!.CreateEventStore()) };
            yield return new object[] { new Func<IEventStore>(() => _postgreSqlStoreFixture!.CreateEventStore()) };
        }
    }
}
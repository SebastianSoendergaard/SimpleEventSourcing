using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using Basses.SimpleEventStore.EventSubscriber;
using Basses.SimpleEventStore.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace Basses.SimpleEventStore.Tests.Projections;

public sealed class ProjectionManagerTests
    : IClassFixture<InMemoryStoreFixture>, IClassFixture<FileStoreFixture>, IClassFixture<PostgreSqlStoreFixture>
{
    private static InMemoryStoreFixture? _inMemoryStoreFixture;
    private static FileStoreFixture? _fileStoreFixture;
    private static PostgreSqlStoreFixture? _postgreSqlStoreFixture;

    public ProjectionManagerTests(InMemoryStoreFixture inMemoryStoreFixture, FileStoreFixture fileStoreFixture, PostgreSqlStoreFixture postgreSqlStoreFixture)
    {
        _inMemoryStoreFixture = inMemoryStoreFixture;
        _fileStoreFixture = fileStoreFixture;
        _postgreSqlStoreFixture = postgreSqlStoreFixture;
    }

    [Theory]
    [MemberData(nameof(StateStoreFactory))]
    public async Task CanProjectSynchronousEvent(Func<IProjectorStateStore> stateStoreFactory)
    {
        var eventStore = new InMemoryEventStore();
        var stateStore = stateStoreFactory();
        var register = new ProjectionsRegister();
        IServiceCollection serviceCollection = new ServiceCollection();

        register.RegisterSynchronousProjector<SynchronousProjector>();

        foreach (var projector in register.AllSubscribers)
        {
            serviceCollection.AddScoped(projector);
        }

        serviceCollection.AddSingleton<ProjectorStore>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var projectionManager = new ProjectionManager(eventStore, stateStore, register, serviceProvider);

        var streamId = Guid.NewGuid().ToString();
        var @event = new CreatedEvent(1, "abc");

        await eventStore.AppendEvents(streamId, 1, [@event]);

        var projectorStore = serviceProvider.GetRequiredService<ProjectorStore>();

        Assert.Equal(1, projectorStore.SequenceNumber);
        Assert.Single(projectorStore.Events);
        Assert.Equal(@event, projectorStore.Events.Single().Event);
    }

    private record CreatedEvent(int Id, string Text);

    public class SynchronousProjector(ProjectorStore store) : IProjector
    {
        public string Name => nameof(SynchronousProjector);

        public Task<long> GetSequenceNumber(EventSubscriberProcessingState currentState)
        {
            return Task.FromResult(store.SequenceNumber);
        }

        public Task Update(IEnumerable<EventEntry> events, CancellationToken cancellationToken)
        {
            store.SequenceNumber = events.Last().SequenceNumber;
            store.Events.AddRange(events);
            return Task.CompletedTask;
        }
    }

    public class ProjectorStore()
    {
        public long SequenceNumber { get; set; } = 0;
        public List<EventEntry> Events { get; } = [];
    }

    public static IEnumerable<object[]> StateStoreFactory
    {
        get
        {
            yield return new object[] { new Func<IProjectorStateStore>(() => _inMemoryStoreFixture!.CreateEventStore()) };
            yield return new object[] { new Func<IProjectorStateStore>(() => _fileStoreFixture!.CreateEventStore()) };
            yield return new object[] { new Func<IProjectorStateStore>(() => _postgreSqlStoreFixture!.CreateEventStore()) };
        }
    }
}

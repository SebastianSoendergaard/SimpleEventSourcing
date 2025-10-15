using Basses.SimpleDocumentStore;
using Basses.SimpleEventStore.Enablers;

namespace WebApi.User;

public class PersistedUserProjector : Projector,
    IProjectionEventHandler<UserCreated>,
    IProjectionEventHandler<UserNameChanged>
{
    private readonly Dictionary<Guid, PersistedUserProjection> _userProjections = [];
    private readonly IDocumentStore _db;
    private long _sequenceNumber = 0;

    public PersistedUserProjector(IDocumentStore db)
    {
        _db = db;
    }

    protected override async Task<long> LoadSequenceNumber()
    {
        var state = await _db.GetByIdAsync<PersistedUserProjectorState>(Name);
        _sequenceNumber = state?.SequenceNumber ?? 0;
        return _sequenceNumber;
    }

    protected override async Task UpdateComplete(long sequenceNumber)
    {
        _sequenceNumber = sequenceNumber;

        var state = await _db.GetByIdAsync<PersistedUserProjectorState>(Name);
        if (state == null)
        {
            state = new PersistedUserProjectorState(Name, _sequenceNumber, _userProjections);
            await _db.CreateAsync(state);
        }
        else
        {
            state = new PersistedUserProjectorState(Name, _sequenceNumber, _userProjections);
            await _db.UpdateAsync(state);
        }
    }

    public PersistedUserProjection GetProjection(Guid userId)
    {
        return _userProjections[userId];
    }

    public Task UpdateWith(UserCreated @event, EventData eventData)
    {
        PersistedUserProjection userProjection = new()
        {
            Id = @event.Id,
            Version = eventData.Version,
            LastUpdated = eventData.Timestamp
        };

        _userProjections.Add(userProjection.Id, userProjection);

        return Task.CompletedTask;
    }

    public Task UpdateWith(UserNameChanged @event, EventData eventData)
    {
        var userProjection = _userProjections[new Guid(eventData.StreamId)];
        userProjection.Name = @event.Name;
        userProjection.Version = eventData.Version;
        userProjection.LastUpdated = eventData.Timestamp;

        return Task.CompletedTask;
    }
}

public record PersistedUserProjectorState(string Name, long SequenceNumber, Dictionary<Guid, PersistedUserProjection> Projections);

public class PersistedUserProjection
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; }
}

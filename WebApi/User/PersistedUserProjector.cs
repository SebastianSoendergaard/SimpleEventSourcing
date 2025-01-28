using Basses.SimpleDocumentStore;
using WebApi.Domain;

namespace WebApi.User;

public class PersistedUserProjector : Projector
{
    public static Guid ProjectorId = new("8E2AB560-5B85-44CD-9743-090F9A584363");

    private readonly Dictionary<Guid, PersistedUserProjection> _userProjections = [];
    private readonly ISimpleObjectDb _db;
    private long _sequenceNumber = 0;

    public PersistedUserProjector(ISimpleObjectDb db)
    {
        _db = db;
    }

    protected override Guid GetId()
    {
        return ProjectorId;
    }

    protected override IEnumerable<Type> GetDomainEventTypes()
    {
        return
        [
            typeof(UserCreated),
            typeof(UserNameChanged)
        ];
    }

    protected override async Task<long> GetSequenceNumber()
    {
        var state = await _db.GetByIdAsync<PersistedUserProjectorState>(ProjectorId);
        _sequenceNumber = state?.SequenceNumber ?? 0;
        return _sequenceNumber;
    }

    protected override async Task UpdateComplete(long sequenceNumber)
    {
        _sequenceNumber = sequenceNumber;

        var state = await _db.GetByIdAsync<PersistedUserProjectorState>(ProjectorId);
        if (state == null)
        {
            state = new PersistedUserProjectorState(ProjectorId, _sequenceNumber, _userProjections);
            await _db.CreateAsync(state);
        }
        else
        {
            state = new PersistedUserProjectorState(ProjectorId, _sequenceNumber, _userProjections);
            await _db.UpdateAsync(state);
        }
    }

    public PersistedUserProjection GetProjection(Guid userId)
    {
        return _userProjections[userId];
    }

    public Task UpdateWith(UserCreated @event, Guid streamId, int version, DateTimeOffset timestamp)
    {
        PersistedUserProjection userProjection = new()
        {
            Id = @event.Id,
            Version = version,
            LastUpdated = timestamp
        };

        _userProjections.Add(userProjection.Id, userProjection);

        return Task.CompletedTask;
    }

    public Task UpdateWith(UserNameChanged @event, Guid streamId, int version, DateTimeOffset timestamp)
    {
        var userProjection = _userProjections[streamId];
        userProjection.Name = @event.Name;
        userProjection.Version = version;
        userProjection.LastUpdated = timestamp;

        return Task.CompletedTask;
    }
}

public record PersistedUserProjectorState(Guid Id, long SequenceNumber, Dictionary<Guid, PersistedUserProjection> Projections);

public class PersistedUserProjection
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; }
}

using WebApi.Domain;

namespace WebApi.User;

public class UserProjector : Projector
{
    public static Guid ProjectorId = new("74D64DD5-B86B-4B0B-908A-361FC0AAF1D0");

    private readonly IDictionary<Guid, UserProjection> _userProjections = new Dictionary<Guid, UserProjection>();
    private long _sequenceNumber = 0;

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

    protected override Task<long> GetSequenceNumber()
    {
        return Task.FromResult(_sequenceNumber);
    }

    protected override Task UpdateComplete(long sequenceNumber)
    {
        _sequenceNumber = sequenceNumber;
        return Task.CompletedTask;
    }

    public UserProjection GetProjection(Guid userId)
    {
        return _userProjections[userId];
    }

    public Task UpdateWith(UserCreated @event, Guid streamId, int version, DateTimeOffset timestamp)
    {
        UserProjection userProjection = new()
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

public class UserProjection
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; }
}

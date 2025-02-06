using EventSourcing.Enablers;

namespace WebApi.User;

public class UserProjector : Projector,
    IProjectionEventHandler<UserCreated>,
    IProjectionEventHandler<UserNameChanged>
{
    public static Guid ProjectorId = new("74D64DD5-B86B-4B0B-908A-361FC0AAF1D0");

    private readonly IDictionary<Guid, UserProjection> _userProjections = new Dictionary<Guid, UserProjection>();
    private long _sequenceNumber = 0;

    public override Guid Id => ProjectorId;

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

    public Task UpdateWith(UserCreated @event, EventData eventData)
    {
        UserProjection userProjection = new()
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
        var userProjection = _userProjections[eventData.StreamId];
        userProjection.Name = @event.Name;
        userProjection.Version = eventData.Version;
        userProjection.LastUpdated = eventData.Timestamp;

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

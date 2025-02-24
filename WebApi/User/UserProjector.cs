using Basses.SimpleEventStore.Enablers;

namespace WebApi.User;

public class UserProjector : Projector,
    IProjectionEventHandler<UserCreated>,
    IProjectionEventHandler<UserNameChanged>
{
    private readonly IDictionary<Guid, UserProjection> _userProjections = new Dictionary<Guid, UserProjection>();

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
        var userProjection = _userProjections[new Guid(eventData.StreamId)];
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

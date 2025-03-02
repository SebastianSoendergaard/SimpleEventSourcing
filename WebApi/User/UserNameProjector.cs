using Basses.SimpleEventStore.Enablers;

namespace WebApi.User;

public class UserNameProjector : Projector,
    IProjectionEventHandler<UserNameChanged>
{
    private readonly List<UserNameProjection> _projections = [];

    public IEnumerable<UserNameProjection> GetProjection()
    {
        return _projections;
    }

    public Task UpdateWith(UserNameChanged @event, EventData eventData)
    {
        UserNameProjection change = new()
        {
            Name = @event.Name,
            Time = eventData.Timestamp,
        };
        _projections.Add(change);

        return Task.CompletedTask;
    }
}

public class UserNameProjection
{
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset Time { get; set; }
}

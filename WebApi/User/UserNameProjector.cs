using WebApi.Domain;

namespace WebApi.User;

public class UserNameProjector : Projector
{
    public static Guid ProjectorId = new("325933F7-883B-4E9F-BBFE-F85A3EE4027B");

    private readonly List<UserNameProjection> _projections = [];

    protected override Guid GetId()
    {
        return ProjectorId;
    }

    protected override IEnumerable<Type> GetDomainEventTypes()
    {
        return [typeof(UserNameChanged)];
    }

    public IEnumerable<UserNameProjection> GetProjection()
    {
        return _projections;
    }

    public Task UpdateWith(UserNameChanged @event, Guid streamId, int version, DateTimeOffset timestamp)
    {
        UserNameProjection change = new()
        {
            Name = @event.Name,
            Time = timestamp,
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

using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace WebApi.User;

public class UserRepository : EventSourcedRepository<UserAggregate>
{
    private readonly UserProjector _userProjector;
    private readonly UserNameProjector _userNameProjector;

    public UserRepository(IEventStore eventStore, UserProjector userProjector, UserNameProjector userNameProjector) : base(eventStore)
    {
        _userProjector = userProjector;
        _userNameProjector = userNameProjector;
    }

    public UserProjection GetUserProjection(Guid userId)
    {
        var projection = _userProjector.GetProjection(userId);
        return projection;
    }

    public IEnumerable<UserNameProjection> GetUserNameProjections()
    {
        var projection = _userNameProjector.GetProjection();
        return projection;
    }
}

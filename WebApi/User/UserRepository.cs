using EventSourcing.EventStore;
using EventSourcing.Projections;
using WebApi.Domain;

namespace WebApi.User;

public class UserRepository : EventSourcedRepository<UserAggregate>
{
    private readonly ProjectionManager _projectionManager;

    public UserRepository(IEventStore eventStore, ProjectionManager projectionManager) : base(eventStore, projectionManager)
    {
        _projectionManager = projectionManager;
    }

    public UserProjection GetUserProjection(Guid userId)
    {
        var projector = _projectionManager.GetProjector<UserProjector>(UserProjector.ProjectorId);
        var projection = projector.GetProjection(userId);
        return projection;
    }

    public IEnumerable<UserNameProjection> GetUserNameProjections()
    {
        var projector = _projectionManager.GetProjector<UserNameProjector>(UserNameProjector.ProjectorId);
        var projection = projector.GetProjection();
        return projection;
    }
}

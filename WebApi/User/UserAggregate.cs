using WebApi.Domain;

namespace WebApi.User;

public class UserAggregate : Aggregate
{
    public string Name { get; private set; } = string.Empty;

    public UserAggregate()
    {
        UserCreated @event = new(Guid.NewGuid());
        Apply(@event);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        UserNameChanged @event = new(name);
        Apply(@event);
    }

    public UserAggregate(IEnumerable<IDomainEvent> events) : base(events)
    {
    }

    public void On(UserCreated @event)
    {
        Id = @event.Id;
    }

    public void On(UserNameChanged @event)
    {
        Name = @event.Name;
    }
}


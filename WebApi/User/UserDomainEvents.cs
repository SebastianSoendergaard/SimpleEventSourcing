using WebApi.Domain;

namespace WebApi.User;

public record UserCreated(Guid Id) : IDomainEvent;

public record UserNameChanged(string Name) : IDomainEvent;

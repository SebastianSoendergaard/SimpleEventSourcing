using Basses.SimpleEventStore.Enablers;

namespace WebApi.User;

public record UserCreated(Guid Id) : IDomainEvent;

public record UserNameChanged(string Name) : IDomainEvent;

// public record UserRegistered(Guid Id, string UserName) : IDomainEvent;

// public record UsersFullNameChanged(Guid UserId, string FullName) : IDomainEvent;

// public record UsersBirthdayGiven(Guid UserId, DateOnly Birthday) : IDomainEvent;

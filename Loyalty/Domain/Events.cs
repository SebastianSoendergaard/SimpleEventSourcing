using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

public record MembershipRegisteredEvent(
    Guid MembershipId,
    Guid MemberInformationId,
    Guid ConfirmationId
) : IDomainEvent;

public record MemberNameUpdatedEvent(
    Guid MembershipId,
    Guid MemberInformationId
) : IDomainEvent;

public record MemberEmailUpdatedEvent(
    Guid MembershipId,
    Guid MemberInformationId
) : IDomainEvent;

public record MembershipConfirmedEvent(
    Guid MembershipId,
    Guid MemberInformationId
) : IDomainEvent;

public record MembershipCanceledEvent(
    Guid MembershipId,
    Guid MemberInformationId
) : IDomainEvent;

public record MembershipTransferRequestedEvent(
    Guid OldMembershipId,
    Guid NewMembershipId,
    Guid ConfirmationId
) : IDomainEvent;

public record MembershipTransferConfirmedEvent(
    Guid OldMembershipId,
    Guid NewMembershipId,
    Guid ConfirmationId
) : IDomainEvent;

public record MemberInformationUpdatedEvent(
    Guid MembershipId,
    Guid MemberInformationId,
    int NewMemberInformationVersion
) : IDomainEvent;

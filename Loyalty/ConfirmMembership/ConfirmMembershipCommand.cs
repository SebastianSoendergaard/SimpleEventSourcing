using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.ConfirmMembership;

public record ConfirmMembershipCommand(Guid MembershipId, Guid ConfirmationId);

internal class ConfirmMembershipCommandHandler(MembershipRepository repository)
{
    public async Task Handle(ConfirmMembershipCommand command)
    {
        var membership = await repository.TryGet(command.MembershipId.ToString());
        if (membership != null)
        {
            membership.Confirm(command.ConfirmationId);
            await repository.Update(membership);
        }
    }
}

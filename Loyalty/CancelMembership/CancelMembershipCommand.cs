using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.CancelMembership;

public record CancelMembershipCommand(Guid MembershipId);

public class CancelMembershipCommandHandler(MembershipRepository repository)
{
    public async Task Handle(CancelMembershipCommand command)
    {
        var membership = await repository.TryGet(command.MembershipId.ToString());
        if (membership != null)
        {
            membership.Cancel();
            await repository.Update(membership);
        }
    }
}

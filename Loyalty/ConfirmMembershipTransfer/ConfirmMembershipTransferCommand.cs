using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.ConfirmMembershipTransfer;

public record ConfirmMembershipTransferCommand(Guid OldMembershipId, Guid ConfirmationId);

public class ConfirmMembershipTransferCommandHandler(MembershipRepository repository)
{
    public async Task Handle(ConfirmMembershipTransferCommand command)
    {
        var existingMembership = await repository.TryGet(command.OldMembershipId.ToString());
        if (existingMembership == null)
        {
            return;
        }

        var newMembership = existingMembership.ConfirmTransfer(command.ConfirmationId);
        if (newMembership == null)
        {
            return;
        }

        await repository.Add(newMembership);
        await repository.Update(existingMembership);
    }
}

using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.RequestMembershipTransfer;

public record RequestMembershipTransferCommand(string OldPhoneNumber, string NewPhoneNumber);

public class RequestMembershipTransferCommandHandler(MembershipRepository repository)
{
    public async Task Handle(RequestMembershipTransferCommand command)
    {
        var oldMembershipId = MembershipId.FromPhoneNumber(command.OldPhoneNumber);
        var newMembershipId = MembershipId.FromPhoneNumber(command.NewPhoneNumber);

        var existingMembership = await repository.TryGet(oldMembershipId.Value.ToString());
        if (existingMembership == null)
        {
            return;
        }

        existingMembership.RequestTransferTo(newMembershipId, command.NewPhoneNumber);
        await repository.Update(existingMembership);
    }
}

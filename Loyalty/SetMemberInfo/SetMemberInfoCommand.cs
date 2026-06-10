using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.SetMemberInfo;

public record SetMemberInfoCommand(Guid MembershipId, string Name, string Email);

public class SetMemberInfoHandler(MembershipRepository repository)
{
    public async Task Handle(SetMemberInfoCommand command)
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

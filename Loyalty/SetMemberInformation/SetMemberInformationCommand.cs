using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.SetMemberInformationCommand;

public record SetMemberInformationCommand(Guid MembershipId, string Name, string Email);

internal class SetMemberInformationHandler(MembershipRepository repository)
{
    public async Task Handle(SetMemberInformationCommand command)
    {
        var membership = await repository.TryGet(command.MembershipId.ToString());
        if (membership != null)
        {
            await repository.Update(membership);
        }
    }
}

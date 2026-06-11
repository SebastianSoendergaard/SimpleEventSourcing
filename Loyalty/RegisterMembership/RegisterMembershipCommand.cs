using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.RegisterMembership;

public record RegisterMembershipCommand(string PhoneNumber);

internal class RegisterMembershipCommandHandler(MembershipRepository repository)
{
    public async Task Handle(RegisterMembershipCommand command)
    {
        var membershipId = MembershipId.FromPhoneNumber(command.PhoneNumber);

        var membership = await repository.TryGet(membershipId.Value.ToString());
        if (membership == null)
        {
            membership = new MembershipAggregate(membershipId, command.PhoneNumber);
            await repository.Add(membership);
        }
        else
        {
            // Phone number has earlier been used but was canceled
            // now the same or a new owner of the phone number wants to register it again
            membership.ReRegister(command.PhoneNumber);
            await repository.Update(membership);
        }
    }
}

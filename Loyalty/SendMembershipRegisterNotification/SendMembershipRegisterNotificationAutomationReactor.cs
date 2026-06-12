using Basses.SimpleEventStore.Enablers;
using UnderstandingEventsourcingExample.Framework;
using UnderstandingEventsourcingExample.Loyalty.Domain;

namespace UnderstandingEventsourcingExample.Loyalty.SendMembershipRegisterNotification;

internal class SendMembershipRegisterNotificationAutomationReactor(SendMembershipRegisterNotificationCommandHandler handler, PiiReadRepository repository) : Reactor,
    IReactionEventHandler<MembershipRegisteredEvent>
{
    public async Task ReactOn(MembershipRegisteredEvent @event, EventData eventData)
    {
        var memberInformation = await repository.TryGetPiiData<MemberInformation>(@event.MemberInformationId, eventData.Version);
        if (memberInformation == null || string.IsNullOrEmpty(memberInformation.PhoneNumber))
        {
            return;
        }

        var cmd = new SendMembershipRegisterNotificationCommand(@event.MembershipId, memberInformation.Name ?? "", memberInformation.PhoneNumber);
        await handler.Handle(cmd);
    }
}

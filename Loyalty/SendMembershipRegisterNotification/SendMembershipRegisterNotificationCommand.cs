namespace UnderstandingEventsourcingExample.Loyalty.SendMembershipRegisterNotification;

public record SendMembershipRegisterNotificationCommand(Guid MembershipId, string Name, string PhoneNumber);

internal class SendMembershipRegisterNotificationCommandHandler
{
    public async Task Handle(SendMembershipRegisterNotificationCommand command)
    {
        // Send a SMS with link to confirm the newly registered membership
        var message = $"Hi {command.Name}, welcome to the coffee shop. Confirm your registration by following this link: http//cofeeshop.com/registration/{command.MembershipId}";
        await SendSms(command.PhoneNumber, message);
    }

    private Task SendSms(string phoneNumber, string message)
    {
        // TODO: send sms
        return Task.CompletedTask;
    }
}

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

internal class MemberInformation
{
    public Guid Id { get; private set; }
    public string? Name { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? TransferToPhoneNumber { get; private set; }

    public MemberInformation(string phoneNumber)
    {
        Id = Guid.NewGuid();
        PhoneNumber = phoneNumber;
    }

    public void UpdateInfo(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public void SetPhoneNumberToTransferTo(string phoneNumber)
    {
        TransferToPhoneNumber = phoneNumber;
    }
}

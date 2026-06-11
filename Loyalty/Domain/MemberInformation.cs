namespace UnderstandingEventsourcingExample.Loyalty.Domain;

internal class MemberInformation
{
    public string? Name { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? TransferToPhoneNumber { get; private set; }

    private MemberInformation()
    {
    }

    public MemberInformation(string phoneNumber)
    {
        PhoneNumber = phoneNumber;
    }

    public MemberInformation WithNameAndEmail(string name, string email)
    {
        Name = name;
        Email = email;
        return this;
    }

    public MemberInformation WithPhoneNumberToTransferTo(string phoneNumber)
    {
        TransferToPhoneNumber = phoneNumber;
        return this;
    }
}

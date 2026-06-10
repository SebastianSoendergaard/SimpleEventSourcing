using System.Security.Cryptography;
using System.Text;

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

internal class MembershipId
{
    public Guid Value { get; private set; }

    private MembershipId()
    {
    }

    public static MembershipId FromPhoneNumber(string phonenumber)
    {
        var id = CreateDeterministicGuid(phonenumber);
        return FromId(id);
    }

    public static MembershipId FromId(Guid id)
    {
        return new MembershipId() { Value = id };
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            byte[] guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);

            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

            return new Guid(guidBytes);
        }
    }
}

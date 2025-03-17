using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.AddItem;

public class DeviceFingerPrintCalculator : IDeviceFingerPrintCalculator
{
    public string GetFingerPrint()
    {
        return Guid.NewGuid().ToString();
    }
}

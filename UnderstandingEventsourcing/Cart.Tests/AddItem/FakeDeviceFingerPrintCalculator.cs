using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Tests.AddItem;

internal class FakeDeviceFingerPrintCalculator : IDeviceFingerPrintCalculator
{
    public string FingerPrint { get; set; } = "";

    public string GetFingerPrint()
    {
        return FingerPrint;
    }
}

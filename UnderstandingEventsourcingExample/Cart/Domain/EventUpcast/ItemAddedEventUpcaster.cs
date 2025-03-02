using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;

public class ItemAddedEventUpcaster : IUpcaster
{
#pragma warning disable CS0612 // Type or member is obsolete
    public bool CanUpcast(string eventType)
    {
        return eventType == typeof(ItemAddedEvent).AssemblyQualifiedName;
    }

    public object DoUpcast(string eventType, string eventJson, IEventSerializer serializer)
    {
        var defaultDeviceFingerPrint = "default-fingerprint";


        if (eventType == typeof(ItemAddedEvent).AssemblyQualifiedName)
        {
            var e = (ItemAddedEvent)serializer.Deserialize(eventJson, eventType);
            return new ItemAddedEventV2(
                e.CartId,
                e.Description,
                e.Image,
                e.Price,
                e.ItemId,
                e.ProductId,
                defaultDeviceFingerPrint
            );
        }

        throw new CartException($"No upcaster for: {eventType}");
    }
#pragma warning restore CS0612 // Type or member is obsolete
}

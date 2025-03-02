using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class CartAggregate : Aggregate,
    IDomainEventHandler<CartCreatedEvent>,
    IDomainEventHandler<ItemAddedEventV2>,
    IDomainEventHandler<ItemRemovedEvent>,
    IDomainEventHandler<CartClearedEvent>
{
    private List<Guid> _items = [];

    public CartAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public CartAggregate(Guid cartId)
    {
        Apply(new CartCreatedEvent(cartId));
    }

    public void AddItem(string description, string image, decimal price, Guid itemId, Guid productId, IDeviceFingerPrintCalculator fingerPrintCalculator)
    {
        if (_items.Count >= 3)
        {
            throw new CartException("Can only add 3 items");
        }

        Apply(new ItemAddedEventV2(
            Id,
            description,
            image,
            price,
            itemId,
            productId,
            fingerPrintCalculator.GetFingerPrint()
        ));
    }

    public void RemoveItem(Guid itemId)
    {
        if (!_items.Contains(itemId))
        {
            throw new CartException($"Item {itemId} not in the Cart");
        }

        Apply(new ItemRemovedEvent(Id, itemId));
    }

    public void Clear()
    {
        Apply(new CartClearedEvent(Id));
    }

    public void On(CartCreatedEvent @event)
    {
        Id = @event.CartId;
    }

    public void On(ItemAddedEventV2 @event)
    {
        _items.Add(@event.ItemId);
    }

    public void On(ItemRemovedEvent @event)
    {
        _items.Remove(@event.ItemId);
    }

    public void On(CartClearedEvent @event)
    {
        _items.Clear();
    }
}

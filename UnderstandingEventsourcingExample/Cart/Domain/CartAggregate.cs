using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class CartAggregate : Aggregate,
    IDomainEventHandler<CartCreatedEvent>,
    IDomainEventHandler<ItemAddedEvent>,
    IDomainEventHandler<ItemRemovedEvent>
{
    List<Guid> _items = [];

    public CartAggregate(Guid id)
    {
        Apply(new CartCreatedEvent(id));
    }

    public void AddItem(string description, string image, decimal price, Guid itemId, Guid productId)
    {
        if (_items.Count >= 3)
        {
            throw new CartException("Can only add 3 items");
        }

        Apply(new ItemAddedEvent(Id, description, image, price, itemId, productId));
    }

    public void RemoveItem(Guid itemId)
    {
        if (!_items.Contains(itemId))
        {
            throw new CartException($"Item {itemId} not in the Cart");
        }

        Apply(new ItemRemovedEvent(Id, itemId));
    }

    public void On(CartCreatedEvent @event)
    {
        Id = @event.CartId;
    }

    public void On(ItemAddedEvent @event)
    {
        _items.Add(@event.ItemId);
    }

    public void On(ItemRemovedEvent @event)
    {
        _items.Remove(@event.ItemId);
    }
}

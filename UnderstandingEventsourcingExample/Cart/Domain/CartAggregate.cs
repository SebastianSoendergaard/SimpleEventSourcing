using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class CartAggregate : Aggregate,
    IDomainEventHandler<CartCreatedEvent>,
    IDomainEventHandler<ItemAddedEventV2>,
    IDomainEventHandler<ItemRemovedEvent>,
    IDomainEventHandler<CartClearedEvent>,
    IDomainEventHandler<CartSubmittedEvent>
{
    private Dictionary<Guid, Guid> _items = [];
    private Dictionary<Guid, decimal> _productPrices = [];
    private bool _isSubmitted = false;

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
            new Guid(Id),
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
        if (!_items.Keys.Contains(itemId))
        {
            throw new CartException($"Item {itemId} not in the Cart");
        }

        Apply(new ItemRemovedEvent(new Guid(Id), itemId));
    }

    public void Clear()
    {
        Apply(new CartClearedEvent(new Guid(Id)));
    }

    public void Submit()
    {
        if (!_items.Any())
        {
            throw new CartException($"Can not submit empty cart");
        }

        if (_isSubmitted)
        {
            throw new CartException($"Can not submit cart twice");
        }

        var orderedProducts = _items.Select(x => new OrderedProduct(x.Value, _productPrices[x.Value])).ToArray();

        Apply(new CartSubmittedEvent(new Guid(Id), orderedProducts, orderedProducts.Sum(x => x.Price)));
    }

    public void On(CartCreatedEvent @event)
    {
        Id = @event.CartId.ToString();
    }

    public void On(ItemAddedEventV2 @event)
    {
        _items.Add(@event.ItemId, @event.ProductId);
        _productPrices[@event.ProductId] = @event.Price;
    }

    public void On(ItemRemovedEvent @event)
    {
        _items.Remove(@event.ItemId);
    }

    public void On(CartClearedEvent @event)
    {
        _items.Clear();
    }

    public void On(CartSubmittedEvent @event)
    {
        _isSubmitted = true;
    }
}

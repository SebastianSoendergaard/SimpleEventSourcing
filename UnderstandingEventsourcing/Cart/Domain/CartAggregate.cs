using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public class CartAggregate : Aggregate,
    IDomainEventHandler<CartCreatedEvent>,
    IDomainEventHandler<ItemAddedEventV2>,
    IDomainEventHandler<ItemRemovedEvent>,
    IDomainEventHandler<ItemArchivedEvent>,
    IDomainEventHandler<CartClearedEvent>,
    IDomainEventHandler<CartSubmittedEvent>,
    IDomainEventHandler<CartPublishedEvent>,
    IDomainEventHandler<CartPublicationFailedEvent>
{
    private Dictionary<Guid, Guid> _items = [];
    private Dictionary<Guid, decimal> _productPrices = [];
    private bool _isSubmitted = false;
    private bool _isPublished = false;
    private bool _isPublicationFailed = false;

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
            CartId: new Guid(Id),
            Description: description,
            Image: image,
            Price: price,
            ItemId: itemId,
            ProductId: productId,
            DeviceFingerPrint: fingerPrintCalculator.GetFingerPrint()
        ));
    }

    public void RemoveItem(Guid itemId)
    {
        if (!_items.Keys.Contains(itemId))
        {
            throw new CartException($"Item {itemId} not in the Cart");
        }

        Apply(new ItemRemovedEvent(CartId: new Guid(Id), ItemId: itemId));
    }

    public void ArchiveItem(Guid productId)
    {
        foreach (var kv in _items)
        {
            if (kv.Value == productId)
            {
                Apply(new ItemArchivedEvent(CartId: new Guid(Id), ItemId: kv.Key));
            }
        }
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

        var orderedProducts = _items.Select(x => new OrderedProduct(ProductId: x.Value, Price: _productPrices[x.Value])).ToArray();

        Apply(new CartSubmittedEvent(CartId: new Guid(Id), OrderedProducts: orderedProducts, TotalPrice: orderedProducts.Sum(x => x.Price)));
    }

    public void Publish()
    {
        if (!_isSubmitted)
        {
            throw new CartException($"Can not publish unsubmitted cart");
        }

        if (_isPublished)
        {
            return;
        }

        Apply(new CartPublishedEvent(new Guid(Id)));
    }

    public void FailPublication()
    {
        Apply(new CartPublicationFailedEvent(new Guid(Id)));
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

    public void On(ItemArchivedEvent @event)
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

    public void On(CartPublishedEvent @event)
    {
        _isPublished = true;
    }

    public void On(CartPublicationFailedEvent @event)
    {
        _isPublicationFailed = true;
    }
}

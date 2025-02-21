using Basses.SimpleEventStore.Enablers;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetCartItems;

public class CartItemsProjector : Projector,
    IProjectionEventHandler<CartCreatedEvent>,
    IProjectionEventHandler<ItemAddedEvent>,
    IProjectionEventHandler<ItemRemovedEvent>,
    IProjectionEventHandler<CartClearedEvent>
{
    public static Guid ProjectorId = new("EF56C1F5-7AD5-463D-AB5C-CF873E7A06F7");

    private Guid? _cartId;
    private decimal _totalPrice = 0;
    private List<CartItem> _items = [];

    public override Guid Id => ProjectorId;

    public CartItemsReadModel? GetReadModel()
    {
        if (_cartId == null)
        {
            return null;
        }

        return new CartItemsReadModel(_cartId.Value, _totalPrice, _items);
    }

    public Task UpdateWith(CartCreatedEvent @event, EventData eventData)
    {
        _cartId = @event.CartId;
        return Task.CompletedTask;
    }

    public Task UpdateWith(ItemAddedEvent @event, EventData eventData)
    {
        var item = new CartItem(
            @event.CartId,
            @event.Description,
            @event.Image,
            @event.Price,
            @event.ItemId,
            @event.ProductId
        );
        _items.Add(item);
        _totalPrice += @event.Price;
        return Task.CompletedTask;
    }

    public Task UpdateWith(ItemRemovedEvent @event, EventData eventData)
    {
        var item = _items.FirstOrDefault(x => x.ItemId == @event.ItemId);
        if (item != null)
        {
            _items.Remove(item);
            _totalPrice -= item.Price;
        }
        return Task.CompletedTask;
    }

    public Task UpdateWith(CartClearedEvent @event, EventData eventData)
    {
        _items.Clear();
        _totalPrice = 0;
        return Task.CompletedTask;
    }
}


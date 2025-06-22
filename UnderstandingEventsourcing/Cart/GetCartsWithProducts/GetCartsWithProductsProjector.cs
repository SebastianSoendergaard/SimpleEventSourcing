using Basses.SimpleEventStore.Enablers;
using Microsoft.Extensions.Options;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetCartsWithProducts;

public class GetCartsWithProductsProjector : Projector,
    IProjectionEventHandler<CartClearedEvent>,
    IProjectionEventHandler<ItemRemovedEvent>,
    IProjectionEventHandler<ItemArchivedEvent>,
    IProjectionEventHandler<ItemAddedEventV2>
{
    private GetCartsWithProductsProjectorRepository? _repository;
    private readonly IOptions<CartOptions> _options;

    public GetCartsWithProductsProjector(IOptions<CartOptions> options)
    {
        _options = options;
    }

    public async Task<GetCartsWithProductsReadModel> GetReadModel(Guid ProductId)
    {
        var cartsWithProducts = await GetRepository().GetByProductId(ProductId);
        return new GetCartsWithProductsReadModel(cartsWithProducts);
    }

    protected override Task<long> LoadSequenceNumber()
    {
        return GetRepository().GetLastProcessedSequenceNumber();
    }

    public Task UpdateWith(CartClearedEvent @event, EventData eventData)
    {
        return GetRepository().RemoveAllItemsFromCart(@event.CartId, eventData.SequenceNumber);
    }

    public Task UpdateWith(ItemRemovedEvent @event, EventData eventData)
    {
        return GetRepository().RemoveItemFromCart(@event.CartId, @event.ItemId, eventData.SequenceNumber);
    }

    public Task UpdateWith(ItemArchivedEvent @event, EventData eventData)
    {
        return GetRepository().RemoveItemFromCart(@event.CartId, @event.ItemId, eventData.SequenceNumber);
    }

    public Task UpdateWith(ItemAddedEventV2 @event, EventData eventData)
    {
        return GetRepository().AddProductToCart(@event.CartId, @event.ItemId, @event.ProductId, eventData.SequenceNumber);
    }

    private GetCartsWithProductsProjectorRepository GetRepository()
    {
        if (_repository == null)
        {
            _repository = new GetCartsWithProductsProjectorRepository(Name, _options.Value.ConnectionString, _options.Value.Schema);
        }
        return _repository;
    }
}


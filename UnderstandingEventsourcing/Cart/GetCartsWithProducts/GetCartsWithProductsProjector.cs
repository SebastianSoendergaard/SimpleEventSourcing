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
    private readonly GetCartsWithProductsProjectorRepository _repository;

    public GetCartsWithProductsProjector(IOptions<CartOptions> options)
    {
        _repository = new GetCartsWithProductsProjectorRepository(Name, options.Value.ConnectionString, options.Value.Schema);
    }

    public async Task<GetCartsWithProductsReadModel> GetReadModel(Guid ProductId)
    {
        var cartsWithProducts = await _repository.GetByProductId(ProductId);
        return new GetCartsWithProductsReadModel(cartsWithProducts);
    }

    protected override Task<long> LoadSequenceNumber()
    {
        return _repository.GetLastProcessedSequenceNumber();
    }

    public Task UpdateWith(CartClearedEvent @event, EventData eventData)
    {
        return _repository.RemoveAllItemsFromCart(@event.CartId, eventData.SequenceNumber);
    }

    public Task UpdateWith(ItemRemovedEvent @event, EventData eventData)
    {
        return _repository.RemoveItemFromCart(@event.CartId, @event.ItemId, eventData.SequenceNumber);
    }

    public Task UpdateWith(ItemArchivedEvent @event, EventData eventData)
    {
        return _repository.RemoveItemFromCart(@event.CartId, @event.ItemId, eventData.SequenceNumber);
    }

    public Task UpdateWith(ItemAddedEventV2 @event, EventData eventData)
    {
        return _repository.AddProductToCart(@event.CartId, @event.ItemId, @event.ProductId, eventData.SequenceNumber);
    }

    protected override Task UpdateComplete(long sequenceNumber)
    {
        return _repository.SetLastProcessedSequenceNumber(sequenceNumber);
    }
}


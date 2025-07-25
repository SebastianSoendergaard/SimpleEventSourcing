using Basses.SimpleEventStore.Enablers;
using Microsoft.Extensions.Options;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetInventory;

public class GetInventoryProjector : Projector,
    IProjectionEventHandler<InventoryChangedEvent>
{
    private readonly GetInventoryProjectorRepository _repository;
    private List<InventoryReadModel> _inventories = [];

    public GetInventoryProjector(IOptions<CartOptions> options)
    {
        _repository = new GetInventoryProjectorRepository(Name, options.Value.ConnectionString, options.Value.Schema);
    }

    public Task<InventoryReadModel> GetReadModel(Guid ProductId)
    {
        return _repository.GetByProductId(ProductId);
    }

    protected override Task<long> LoadSequenceNumber()
    {
        return _repository.GetLastProcessedSequenceNumber();
    }

    protected override Task UpdateStarting()
    {
        _inventories.Clear();
        return Task.CompletedTask;
    }

    protected override Task UpdateComplete(long sequenceNumber)
    {
        return _repository.Upsert(sequenceNumber, _inventories);
    }

    public Task UpdateWith(InventoryChangedEvent @event, EventData eventData)
    {
        _inventories.Add(new InventoryReadModel(@event.ProductId, @event.Inventory));
        return Task.CompletedTask;
    }
}


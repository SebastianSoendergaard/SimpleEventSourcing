using Basses.SimpleEventStore.Enablers;
using Microsoft.Extensions.Options;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.GetInventory;

public class GetInventoryProjector : Projector,
    IProjectionEventHandler<InventoryChangedEvent>, IDisposable
{
    private GetInventoryProjectorRepository? _repository;
    private List<InventoryReadModel> _inventories = [];
    private readonly IOptions<CartOptions> _options;

    public GetInventoryProjector(IOptions<CartOptions> options)
    {
        _options = options;
    }

    public Task<InventoryReadModel> GetReadModel(Guid ProductId)
    {
        return GetRepository().GetByProductId(ProductId);
    }

    protected override Task<long> LoadSequenceNumber()
    {
        return GetRepository().GetLastProcessedSequenceNumber();
    }

    protected override Task UpdateStarting()
    {
        _inventories.Clear();
        return Task.CompletedTask;
    }

    protected override Task UpdateComplete(long sequenceNumber)
    {
        return GetRepository().Upsert(sequenceNumber, _inventories);
    }

    public Task UpdateWith(InventoryChangedEvent @event, EventData eventData)
    {
        _inventories.Add(new InventoryReadModel(@event.ProductId, @event.Inventory));
        return Task.CompletedTask;
    }

    private GetInventoryProjectorRepository GetRepository()
    {
        if (_repository == null)
        {
            _repository = new GetInventoryProjectorRepository(Name, _options.Value.ConnectionString, _options.Value.Schema);
        }
        return _repository;
    }

    public void Dispose()
    {
        _repository?.Dispose();
    }
}


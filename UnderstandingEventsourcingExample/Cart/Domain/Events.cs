using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public record CartCreatedEvent(
    Guid CartId
) : IDomainEvent;

public record ItemAddedEvent(
    Guid CartId,
    string Description,
    string Image,
    decimal Price,
    Guid ItemId,
    Guid ProductId
) : IDomainEvent;

public record ItemRemovedEvent(
    Guid CartId,
    Guid ItemId
) : IDomainEvent;

public record CartClearedEvent(
    Guid CartId
) : IDomainEvent;

public record InventoryChangedEvent(
    Guid ProductId,
    int Inventory
) : IDomainEvent;

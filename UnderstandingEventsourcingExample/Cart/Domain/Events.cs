using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Cart.Domain;

public record CartCreatedEvent(
    Guid CartId
) : IDomainEvent;

public record ItemAddedEventV2(
    Guid CartId,
    string Description,
    string Image,
    decimal Price,
    Guid ItemId,
    Guid ProductId,
    // since V2
    string DeviceFingerPrint
) : IDomainEvent;

public record ItemRemovedEvent(
    Guid CartId,
    Guid ItemId
) : IDomainEvent;

public record ItemArchivedEvent(
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

public record PriceChangedEvent(
    Guid ProductId,
    decimal NewPrice,
    decimal OldPrice
) : IDomainEvent;

public record OrderedProduct(Guid ProductId, decimal Price);
public record CartSubmittedEvent(
    Guid CartId,
    OrderedProduct[] OrderedProducts,
    decimal TotalPrice
) : IDomainEvent;

#region [          DEPRECATED          ]

[Obsolete]
public record ItemAddedEvent(
    Guid CartId,
    string Description,
    string Image,
    decimal Price,
    Guid ItemId,
    Guid ProductId
) : IDomainEvent;

#endregion

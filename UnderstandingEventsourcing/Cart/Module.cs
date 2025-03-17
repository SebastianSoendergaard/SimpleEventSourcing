using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;
using Basses.SimpleEventStore.Projections;
using Basses.SimpleMessageBus;
using Basses.SimpleMessageBus.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnderstandingEventsourcingExample.Cart.AddItem;
using UnderstandingEventsourcingExample.Cart.ChangeInventory;
using UnderstandingEventsourcingExample.Cart.ChangePrice;
using UnderstandingEventsourcingExample.Cart.ClearCart;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;
using UnderstandingEventsourcingExample.Cart.GetCartItems;
using UnderstandingEventsourcingExample.Cart.GetCartsWithProducts;
using UnderstandingEventsourcingExample.Cart.GetInventory;
using UnderstandingEventsourcingExample.Cart.Infrastructure.Kafka;
using UnderstandingEventsourcingExample.Cart.Infrastructure.Migration;
using UnderstandingEventsourcingExample.Cart.RemoveItem;

namespace UnderstandingEventsourcingExample.Cart;

public static class Module
{
    public static IServiceCollection AddCartModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CartOptions>(configuration.GetSection("Cart:EventStore"));
        services.Configure<KafkaOptions>(configuration.GetSection("Cart:Kafka"));

        var connectionString = configuration.GetValue<string>("Cart:EventStore:ConnectionString") ?? "";
        var schema = configuration.GetValue<string>("Cart:EventStore:Schema") ?? "";
        var eventStoreName = configuration.GetValue<string>("Cart:EventStore:EventStoreName") ?? "";
        var projectorStateStoreName = configuration.GetValue<string>("Cart:EventStore:ProjectorStateStoreName") ?? "";

        var kafkaServer = configuration.GetValue<string>("Cart:Kafka:Server") ?? "";
        var kafkaClientId = configuration.GetValue<string>("Cart:Kafka:ClientId") ?? "";

        services.AddSingleton<IEventStore>(x => new PostgreSqlEventStore(connectionString, schema, eventStoreName));
        services.AddSingleton<IProjectorStateStore>(x => new PostgreSqlProjectorStateStore(connectionString, schema, projectorStateStoreName));
        services.AddSingleton<ProjectionManager>();

        services.AddKafkaMessageBus();

        services.AddScoped<AddItemCommandHandler>();
        services.AddScoped<RemoveItemCommandHandler>();
        services.AddScoped<ArchiveItemCommandHandler>();
        services.AddScoped<ClearCartCommandHandler>();
        services.AddScoped<GetCartItemsQueryHandler>();
        services.AddScoped<ChangeInventoryCommandHandler>();
        services.AddScoped<GetInventoryQueryHandler>();
        services.AddScoped<ChangePriceCommandHandler>();
        services.AddScoped<GetCartsWithProductsQueryHandler>();
        services.AddScoped<SubmitCartCommandHandler>();

        ReadModelMigrator.Migrate(connectionString);
        services.AddScoped<GetInventoryProjector>();
        services.AddScoped<GetCartsWithProductsProjector>();

        services.AddScoped<CartRepository>();
        services.AddScoped<InventoryRepository>();
        services.AddScoped<PriceRepository>();

        services.AddScoped<IDeviceFingerPrintCalculator, DeviceFingerPrintCalculator>();

        return services;
    }

    public static void UseCartModule(this IHost host)
    {
        var eventStore = host.Services.GetRequiredService<IEventStore>();
        eventStore.RegisterUpcaster(new ItemAddedEventUpcaster());

        var projectionManager = host.Services.GetRequiredService<ProjectionManager>();
        projectionManager.RegisterSynchronousProjector<GetInventoryProjector>();
        projectionManager.RegisterSynchronousProjector<GetCartsWithProductsProjector>();

        var messageConsumer = host.Services.GetRequiredService<IMessageConsumer>();
        messageConsumer.Subscribe<ExternalInventoryChangedEvent>("understand-eventsourcing-topic", "inventory-changed", async e =>
        {
            await host.ExecuteScoped<ChangeInventoryCommandHandler>(h => h.Handle(new ChangeInventoryCommand(e.ProductId, e.Inventory)));
        });
        messageConsumer.Subscribe<ExternalPriceChangedEvent>("understand-eventsourcing-topic", "price-changed", async e =>
        {
            await host.ExecuteScoped<ChangePriceCommandHandler>(h => h.Handle(new ChangePriceCommand(e.ProductId, e.NewPrice, e.OldPrice)));
        });
    }

    public static void RegisterCartModuleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cart/add-item/v1", async ([FromServices] AddItemCommandHandler handler, [FromBody] AddItemCommand cmd) => await handler.Handle(cmd));
        app.MapPost("/api/cart/remove-item/v1", async ([FromServices] RemoveItemCommandHandler handler, [FromBody] RemoveItemCommand cmd) => await handler.Handle(cmd));
        app.MapPost("/api/cart/archive-item/v1", async ([FromServices] ArchiveItemCommandHandler handler, [FromBody] ArchiveItemCommand cmd) => await handler.Handle(cmd));
        app.MapPost("/api/cart/clear-cart/v1", async ([FromServices] ClearCartCommandHandler handler, [FromBody] ClearCartCommand cmd) => await handler.Handle(cmd));
        app.MapGet("/api/cart/get-items/v1", async ([FromServices] GetCartItemsQueryHandler handler, [FromQuery] Guid cartId) => await handler.Handle(new GetCartItemsQuery(cartId)));
        app.MapPost("/api/cart/submit-cart/v1", async ([FromServices] SubmitCartCommandHandler handler, [FromBody] SubmitCartCommand cmd) => await handler.Handle(cmd));
        app.MapGet("/api/cart/get-inventory/v1", async ([FromServices] GetInventoryQueryHandler handler, [FromQuery] Guid productId) => await handler.Handle(new GetInventoryQuery(productId)));
        app.MapGet("/api/cart/get-cartswithproducts/v1", async ([FromServices] GetCartsWithProductsQueryHandler handler, [FromQuery] Guid productId) => await handler.Handle(new GetCartsWithProductsQuery(productId)));

        app.MapPost("/api/external/change-inventory/v1", async ([FromServices] IMessageProducer messageProducer, [FromBody] ExternalInventoryChangedEvent e) => await messageProducer.SendMessage("understand-eventsourcing-topic", "inventory-changed", e));
        app.MapPost("/api/external/change-price/v1", async ([FromServices] IMessageProducer messageProducer, [FromBody] ExternalPriceChangedEvent e) => await messageProducer.SendMessage("understand-eventsourcing-topic", "price-changed", e));

        app.MapGet("/api/support/get-aggregate-events/v1", async ([FromServices] IEventStore eventStore, [FromQuery] string aggregateId) => await eventStore.LoadEvents(aggregateId));
        app.MapGet("/api/support/get-latest-events/v1", async ([FromServices] IEventStore eventStore, [FromQuery] int eventMaxCount) =>
        {
            var head = (await eventStore.GetHeadSequenceNumber()) + 1; // fix offset
            return await eventStore.LoadEvents(head - eventMaxCount, eventMaxCount);
        });
        app.MapGet("/api/support/get-projector-states/v1", async ([FromServices] IEventStore eventStore, [FromServices] ProjectionManager projectorManager, [FromServices] IServiceProvider serviceProvider) =>
        {
            var projectorTypes = projectorManager.GetProjectorTypes();
            var eventStoreHeadSequenceNumber = await eventStore.GetHeadSequenceNumber();
            var projectorStates = new List<object>();

            foreach (var projectorType in projectorTypes)
            {
                var projector = (IProjector)serviceProvider.GetRequiredService(projectorType);

                var projectorHeadSequenceNumber = await projector.GetSequenceNumber();
                var processingState = await projectorManager.GetProcessingState(projector);

                projectorStates.Add(new
                {
                    projector.Name,
                    eventStoreHeadSequenceNumber,
                    projectorHeadSequenceNumber,
                    processingState
                });
            }

            return projectorStates;
        });
    }
}

internal static class Extentions
{
    public static async Task ExecuteScoped<T>(this IHost host, Func<T, Task> onExecute)
    {
        using var scope = host.Services.CreateScope();
        var commandHandler = (T)scope.ServiceProvider.GetRequiredService(typeof(T));
        await onExecute(commandHandler);
    }
}

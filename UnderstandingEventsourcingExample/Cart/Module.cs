using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;
using Basses.SimpleEventStore.Projections;
using Microsoft.AspNetCore.Mvc;
using UnderstandingEventsourcingExample.Cart.AddItem;
using UnderstandingEventsourcingExample.Cart.ChangeInventory;
using UnderstandingEventsourcingExample.Cart.ClearCart;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;
using UnderstandingEventsourcingExample.Cart.GetCartItems;
using UnderstandingEventsourcingExample.Cart.GetInventory;
using UnderstandingEventsourcingExample.Cart.Migration;
using UnderstandingEventsourcingExample.Cart.RemoveItem;

namespace UnderstandingEventsourcingExample.Cart;

public static class Module
{
    public static IServiceCollection AddCartModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CartOptions>(configuration.GetSection("Cart"));

        var connectionString = configuration.GetValue<string>("Cart:ConnectionString") ?? "";
        var schema = configuration.GetValue<string>("Cart:Schema") ?? "";
        var eventStoreName = configuration.GetValue<string>("Cart:EventStoreName") ?? "";
        var projectorStateStoreName = configuration.GetValue<string>("Cart:ProjectorStateStoreName") ?? "";

        services.AddSingleton<IEventStore>(x => new PostgreSqlEventStore(connectionString, schema, eventStoreName));
        services.AddSingleton<IProjectorStateStore>(x => new PostgreSqlProjectorStateStore(connectionString, schema, projectorStateStoreName));
        services.AddSingleton<ProjectionManager>();

        services.AddScoped<AddItemCommandHandler>();
        services.AddScoped<RemoveItemCommandHandler>();
        services.AddScoped<ClearCartCommandHandler>();
        services.AddScoped<GetCartItemsQueryHandler>();
        services.AddScoped<ChangeInventoryCommandHandler>();
        services.AddScoped<GetInventoryQueryHandler>();
        services.AddScoped<SubmitCartCommandHandler>();

        ReadModelMigrator.Migrate(connectionString);
        services.AddScoped<GetInventoryProjector>();

        services.AddScoped<CartRepository>();
        services.AddScoped<InventoryRepository>();

        services.AddScoped<IDeviceFingerPrintCalculator, DeviceFingerPrintCalculator>();

        return services;
    }

    public static void UseCartModule(this IHost host)
    {
        var eventStore = host.Services.GetRequiredService<IEventStore>();
        eventStore.RegisterUpcaster(new ItemAddedEventUpcaster());

        var projectionManager = host.Services.GetRequiredService<ProjectionManager>();
        projectionManager.RegisterSynchronousProjector<GetInventoryProjector>();

        //projectionManager.Start();
    }

    public static void RegisterCartModuleEndpoints(this WebApplication app)
    {
        app.MapPost("/api/cart/add-item/v1", async ([FromServices] AddItemCommandHandler handler, [FromBody] AddItemCommand cmd) => await handler.Handle(cmd));
        app.MapPost("/api/cart/remove-item/v1", async ([FromServices] RemoveItemCommandHandler handler, [FromBody] RemoveItemCommand cmd) => await handler.Handle(cmd));
        app.MapPost("/api/cart/clear-cart/v1", async ([FromServices] ClearCartCommandHandler handler, [FromBody] ClearCartCommand cmd) => await handler.Handle(cmd));
        app.MapGet("/api/cart/get-items/v1", async ([FromServices] GetCartItemsQueryHandler handler, [FromQuery] Guid cartId) => await handler.Handle(new GetCartItemsQuery(cartId)));
        app.MapPost("/api/cart/submit-cart/v1", async ([FromServices] SubmitCartCommandHandler handler, [FromBody] SubmitCartCommand cmd) => await handler.Handle(cmd));

        app.MapPost("/api/inventories/change-inventory/v1", async ([FromServices] ChangeInventoryCommandHandler handler, [FromBody] ChangeInventoryCommand cmd) => await handler.Handle(cmd));
        app.MapGet("/api/inventories/get-inventory/v1", async ([FromServices] GetInventoryQueryHandler handler, [FromQuery] Guid productId) => await handler.Handle(new GetInventoryQuery(productId)));

        app.MapGet("/api/support/get-events/v1", async ([FromServices] IEventStore eventStore, [FromQuery] string aggregateId) => await eventStore.LoadEvents(aggregateId));
    }
}

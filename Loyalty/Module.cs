using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.Projections;
using Basses.SimpleEventStore.Reactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnderstandingEventsourcingExample.Cart.Infrastructure.Kafka;

namespace UnderstandingEventsourcingExample.Loyalty;

public static class Module
{
    public static IServiceCollection AddLoyaltyModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LoyaltyOptions>(configuration.GetSection("Loyalty:EventStore"));
        services.Configure<KafkaOptions>(configuration.GetSection("Loyalty:Kafka"));

        var connectionString = configuration.GetValue<string>("Loyalty:EventStore:ConnectionString") ?? "";
        var schema = configuration.GetValue<string>("Loyalty:EventStore:Schema") ?? "";
        var eventStoreName = configuration.GetValue<string>("Loyalty:EventStore:EventStoreName") ?? "";
        var projectorStateStoreName = configuration.GetValue<string>("Loyalty:EventStore:ProjectorStateStoreName") ?? "";
        var reactorStateStoreName = configuration.GetValue<string>("Loyalty:EventStore:ReactorStateStoreName") ?? "";

        var kafkaServer = configuration.GetValue<string>("Loyalty:Kafka:Server") ?? "";
        var kafkaClientId = configuration.GetValue<string>("Loyalty:Kafka:ClientId") ?? "";

        //services.AddEventStore(
        //    _ => new PostgreSqlEventStore(connectionString, schema, eventStoreName),
        //    r => r
        //    .RegisterUpcaster(new ItemAddedEventUpcaster())
        //);
        //services.AddProjections(
        //    _ => new PostgreSqlProjectorStateStore(connectionString, schema, projectorStateStoreName),
        //    r => r
        //    .RegisterAsynchronousProjector<GetInventoryProjector>()
        //    .RegisterAsynchronousProjector<GetCartsWithProductsProjector>()
        //);
        //services.AddReactions(
        //    _ => new PostgreSqlReactorStateStore(connectionString, schema, reactorStateStoreName),
        //    r => r
        //    .RegisterAsynchronousReactor<ArchiveItemAutomationReactor>()
        //    .RegisterAsynchronousReactor<PublishCartAutomationReactor>()
        //);

        //services.AddKafkaMessageBus();

        //services.AddScoped<AddItemCommandHandler>();
        //services.AddScoped<RemoveItemCommandHandler>();
        //services.AddScoped<ArchiveItemCommandHandler>();
        //services.AddScoped<ClearCartCommandHandler>();
        //services.AddScoped<GetCartItemsQueryHandler>();
        //services.AddScoped<ChangeInventoryCommandHandler>();
        //services.AddScoped<GetInventoryQueryHandler>();
        //services.AddScoped<ChangePriceCommandHandler>();
        //services.AddScoped<GetCartsWithProductsQueryHandler>();
        //services.AddScoped<SubmitCartCommandHandler>();
        //services.AddScoped<PublishCartCommandHandler>();

        //ReadModelMigrator.Migrate(connectionString);

        //services.AddScoped<CartRepository>();
        //services.AddScoped<InventoryRepository>();
        //services.AddScoped<PricingRepository>();

        //services.AddScoped<IDeviceFingerPrintCalculator, DeviceFingerPrintCalculator>();

        return services;
    }

    public static void UseLoyaltyModule(this IHost host)
    {
        //var messageConsumer = host.Services.GetRequiredService<IMessageConsumer>();
        //messageConsumer.Subscribe<ExternalInventoryChangedEvent>("understand-eventsourcing-topic", "inventory-changed", async e =>
        //{
        //    await host.ExecuteScoped<ChangeInventoryCommandHandler>(h => h.Handle(new ChangeInventoryCommand(e.ProductId, e.Inventory)));
        //});
        //messageConsumer.Subscribe<ExternalPriceChangedEvent>("understand-eventsourcing-topic", "price-changed", async e =>
        //{
        //    await host.ExecuteScoped<ChangePriceCommandHandler>(h => h.Handle(new ChangePriceCommand(e.ProductId, e.NewPrice, e.OldPrice)));
        //});
    }

    public static void RegisterLoyaltyModuleEndpoints(this IEndpointRouteBuilder app)
    {
        //app.MapPost("/api/cart/items/add/v1", async ([FromServices] AddItemCommandHandler handler, [FromBody] AddItemCommand cmd) => await handler.Handle(cmd));

        app.MapGet("/api/loyalty/support/get-aggregate-events/v1", async ([FromServices] IEventStore eventStore, [FromQuery] string aggregateId) => await eventStore.LoadEvents(aggregateId));
        app.MapGet("/api/loyalty/support/get-latest-events/v1", async ([FromServices] IEventStore eventStore, [FromQuery] int eventMaxCount) =>
        {
            var head = (await eventStore.GetHeadSequenceNumber()) + 1; // fix offset
            return await eventStore.LoadEvents(head - eventMaxCount, eventMaxCount);
        });
        app.MapGet("/api/loyalty/support/get-projector-states/v1", async ([FromServices] IEventStore eventStore, [FromServices] ProjectionManager projectionManager, [FromServices] IServiceProvider serviceProvider) =>
        {
            var projectorTypes = projectionManager.GetProjectorTypes();
            var eventStoreHeadSequenceNumber = await eventStore.GetHeadSequenceNumber();
            var projectorStates = new List<object>();

            foreach (var projectorType in projectorTypes)
            {
                var projector = (IProjector)serviceProvider.GetRequiredService(projectorType);

                var processingState = await projectionManager.GetProcessingState(projector);
                var projectorHeadSequenceNumber = await projector.GetSequenceNumber(processingState);

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
        app.MapGet("/api/loyalty/support/get-reactor-states/v1", async ([FromServices] IEventStore eventStore, [FromServices] ReactionManager reactionManager, [FromServices] IServiceProvider serviceProvider) =>
        {
            var reactorTypes = reactionManager.GetReactorTypes();
            var eventStoreHeadSequenceNumber = await eventStore.GetHeadSequenceNumber();
            var reactorStates = new List<object>();

            foreach (var reactorType in reactorTypes)
            {
                var reactor = (IReactor)serviceProvider.GetRequiredService(reactorType);

                var processingState = await reactionManager.GetProcessingState(reactor);
                var reactorHeadSequenceNumber = await reactor.GetSequenceNumber(processingState);

                reactorStates.Add(new
                {
                    reactor.Name,
                    eventStoreHeadSequenceNumber,
                    reactorHeadSequenceNumber,
                    processingState
                });
            }

            return reactorStates;
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

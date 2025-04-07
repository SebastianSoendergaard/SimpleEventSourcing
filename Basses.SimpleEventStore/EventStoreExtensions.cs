using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;
using Basses.SimpleEventStore.Projections;
using Basses.SimpleEventStore.Reactions;
using Microsoft.Extensions.DependencyInjection;

namespace Basses.SimpleEventStore;

public static class EventStoreExtensions
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, Func<IServiceProvider, IEventStore> eventStoreFactory, Action<UpcasterRegister>? registerUpcasters = null)
    {
        services.AddSingleton(x =>
        {
            var eventStore = eventStoreFactory(x);

            UpcasterRegister register = new UpcasterRegister();
            registerUpcasters?.Invoke(register);
            foreach (var upcaster in register.Upcasters)
            {
                eventStore.RegisterUpcaster(upcaster);
            }

            return eventStore;
        });
        return services;
    }

    public static IServiceCollection AddProjections(this IServiceCollection services, Func<IServiceProvider, IProjectorStateStore> stateStoreFactory, Action<ProjectionsRegister>? registerProjections = null)
    {
        ProjectionsRegister register = new ProjectionsRegister();
        registerProjections?.Invoke(register);

        foreach (var projectorType in register.AllSubscribers)
        {
            services.AddScoped(projectorType);
        }

        services.AddSingleton(register);
        services.AddSingleton(x => stateStoreFactory(x));
        services.AddSingleton<ProjectionManager>();
        services.AddHostedService<ProjectionManagerBackgroundService>();
        return services;
    }

    public static IServiceCollection AddReactions(this IServiceCollection services, Func<IServiceProvider, IReactorStateStore> stateStoreFactory, Action<ReactionsRegister>? registerReactions = null)
    {
        ReactionsRegister register = new ReactionsRegister();
        registerReactions?.Invoke(register);

        foreach (var reactorType in register.AllSubscribers)
        {
            services.AddScoped(reactorType);
        }

        services.AddSingleton(register);
        services.AddSingleton(x => stateStoreFactory(x));
        services.AddSingleton<ReactionManager>();
        services.AddHostedService<ReactionManagerBackgroundService>();
        return services;
    }
}

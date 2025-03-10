using Microsoft.Extensions.DependencyInjection;
using UnderstandingEventsourcingExample.Cart.Infrastructure.Kafka;

namespace Basses.SimpleMessageBus.Kafka;

public static class KafkaExtentions
{
    public static IServiceCollection AddKafkaMessageBus(this IServiceCollection services)
    {
        services.AddSingleton<IMessageProducer, KafkaProducer>();
        services.AddSingleton<IMessageConsumer, KafkaConsumer>();
        services.AddHostedService<MessageConsumerBackgroundService>();
        return services;
    }
}

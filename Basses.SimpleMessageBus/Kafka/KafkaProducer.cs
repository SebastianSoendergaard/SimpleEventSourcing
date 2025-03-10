using System.Text.Json;
using Basses.SimpleMessageBus;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnderstandingEventsourcingExample.Cart.Infrastructure.Kafka;

internal class KafkaProducer : IMessageProducer, IDisposable
{
    private readonly IOptions<KafkaOptions> _options;
    private readonly ILogger<KafkaProducer> _logger;
    private IProducer<Null, string> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.Server,
            ClientId = options.Value.ClientId,
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
        _options = options;
        _logger = logger;
    }

    public async Task<bool> SendMessage<T>(string topic, string messageName, T message) where T : class
    {
        try
        {
            var kafkaMessage = new MessageEnvelope
            {
                MessageName = messageName,
                ProducerName = _options.Value.ProducerName,
                PublishTime = DateTimeOffset.UtcNow,
                Version = 1,
                DeprecationMessage = null,
                Payload = message
            };

            var jsonMessage = JsonSerializer.Serialize(kafkaMessage);
            var report = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = jsonMessage });
            return report.Status == PersistenceStatus.Persisted;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("Stopping Kafka producer");
        _producer.Flush(TimeSpan.FromSeconds(10));
        _logger.LogInformation("Kafka producer stopped");
        _producer.Dispose();
    }
}


using System.Text.Json;
using Basses.SimpleMessageBus;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnderstandingEventsourcingExample.Cart.Infrastructure.Kafka;

internal class KafkaConsumer : IMessageConsumer
{
    private readonly IOptions<KafkaOptions> _options;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly Dictionary<string, KafkaSubscription> _subscriptions = [];
    private DateTime _latestRegistered = DateTime.MaxValue;

    public KafkaConsumer(IOptions<KafkaOptions> options, ILogger<KafkaConsumer> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool Subscribe<T>(string topic, string messageName, Func<T, Task> onMessage) where T : class
    {
        if (IsRegistrationDone())
        {
            throw new InvalidOperationException("All subscribers should be registered within short time");
        }

        var subscription = new KafkaSubscription
        {
            Topic = topic,
            MessageName = messageName,
            MessageType = typeof(T),
            OnEvent = o => onMessage((T)o)
        };

        _latestRegistered = DateTime.Now;

        return _subscriptions.TryAdd($"{topic}+{messageName}", subscription);
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (IsRegistrationDone())
            {
                break;
            }
            await Task.Delay(1000, stoppingToken);
        }

        if (IsRegistrationDone())
        {
            var subscriptions = _subscriptions.Values.ToList();
            var topics = subscriptions.Select(x => x.Topic).ToList();

            _logger.LogInformation("Registration done, setting up consumer for topics: [{Topics}]", string.Join(", ", topics));

            var consumer = CreateConsumer(topics);

            await Consume(consumer, stoppingToken);
        }
    }

    private bool IsRegistrationDone()
    {
        return DateTime.Now.AddSeconds(-1) > _latestRegistered; // Done if no registration within the last sec  
    }

    private IConsumer<Ignore, string> CreateConsumer(IEnumerable<string> topics)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.Value.Server,
            GroupId = _options.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(topics.Distinct());

        return consumer;
    }

    private Task Consume(IConsumer<Ignore, string> consumer, CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = consumer.Consume(stoppingToken);
                        await HandleConsumedMessage(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception while consuming messages: {ErrorMessage}", ex.Message);
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Stopping consumer");
                consumer.Close();
                consumer.Dispose();
            }
        }, stoppingToken);
    }

    private async Task HandleConsumedMessage(ConsumeResult<Ignore, string> message)
    {
        var topic = message.Topic;
        var content = message.Message.Value;

        _logger.LogInformation("Handling consumed message: {Topic}, {Content}", topic, content);

        var kafkaMessage = JsonSerializer.Deserialize<MessageEnvelope>(content) ?? throw new ArgumentException($"Invalid Kafka message: {content}");

        var subscriptions = _subscriptions.Values.ToList();

        var subscription = subscriptions.FirstOrDefault(x => x.Topic == topic && x.MessageName == kafkaMessage.MessageName);
        if (subscription == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(kafkaMessage.DeprecationMessage))
        {
            _logger.LogWarning("Received deprecated event: {Topic}, {Producer}, {MessageName}, {Version}, Message: {DeprecationMessage}",
                message.Topic, kafkaMessage.ProducerName, kafkaMessage.MessageName, kafkaMessage.Version, kafkaMessage.DeprecationMessage);
        }

        var payloadJson = ((JsonElement)kafkaMessage.Payload).GetRawText();

        var payloadInstance = JsonSerializer.Deserialize(payloadJson, subscription.MessageType) ?? throw new ArgumentException($"Invalid Kafka payload: {payloadJson}");

        await subscription.OnEvent(payloadInstance);
    }

    private class KafkaSubscription
    {
        public required string Topic { get; init; }
        public required string MessageName { get; init; }
        public required Type MessageType { get; init; }
        public required Func<object, Task> OnEvent { get; init; }
    }
}



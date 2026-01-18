using System.Text.Json.Serialization;
using Confluent.Kafka;
using ECommerce.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Shared.Kafka.Consumer;
using Microsoft.Extensions.Options;
using ECommerce.Shared.Kafka.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shared.Kafka.Consumer
{
    public abstract class KafkaConsumerService<TEvent> : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ILogger<KafkaConsumerService<TEvent>> Logger;
        private readonly string _topic;

        protected KafkaConsumerService(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumerService<TEvent>> logger,
            string topic)
        {
            ServiceProvider = serviceProvider;
            Logger = logger;
            _topic = topic;

            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                GroupId = kafkaSettings.Value.Consumer.GroupId,
                AutoOffsetReset = Enum.Parse<AutoOffsetReset>(
                    kafkaSettings.Value.Consumer.AutoOffsetReset ?? "earliest", 
                    ignoreCase: true),
                EnableAutoCommit = kafkaSettings.Value.Consumer.EnableAutoCommit
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topic);
            Logger.LogInformation("Kafka consumer started for topic: {Topic}", _topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);

                        if (consumeResult?.Message?.Value != null)
                        {
                            Logger.LogInformation(
                                "Received message from topic {Topic}, Partition: {Partition}, Offset: {Offset}",
                                consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

                            await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                            _consumer.Commit(consumeResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error processing message from topic {Topic}", _topic);
                        // Don't commit if processing failed
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Kafka consumer for topic {Topic} is stopping", _topic);
            }
            finally
            {
                _consumer.Close();
            }
        }

        private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                var @event = JsonSerializer.Deserialize<TEvent>(message);

                if (@event != null)
                {
                    using var scope = ServiceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<TEvent>>();
                    await handler.HandleAsync(@event, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing event {EventType}:{Message}", typeof(TEvent).Name, message);
                throw; // Rethrow to prevent commit
            }
        }

        public override void Dispose()
        {
            _consumer?.Close();
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}

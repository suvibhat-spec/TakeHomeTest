using System.Text.Json;
using Confluent.Kafka;
using ECommerce.Shared.Kafka.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Shared.Kafka.Producer
{
    public class KafkaProducerService : IDisposable, IKafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<IKafkaProducer> _logger;

        public KafkaProducerService(
            IOptions<KafkaSettings> kafkaSettings,
            ILogger<IKafkaProducer> logger
            )
        {
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                ClientId = kafkaSettings.Value.Producer.ClientId,
                MessageTimeoutMs = kafkaSettings.Value.Producer.MessageTimeoutMs
            };

            _logger.LogInformation("[KafkaProducerService] Connecting to BootstrapServers: {BootstrapServers}", kafkaSettings.Value.BootstrapServers);
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task PublishAsync<TEvent>(string topic, string key, TEvent eventData)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = JsonSerializer.Serialize(eventData)
                };

                var result = await _producer.ProduceAsync(topic, message);

                _logger.LogInformation(
                    "Published event {EventType} to topic {Topic}, Partition: {Partition}, Offset: {Offset}",
                    typeof(TEvent).Name, topic, result.Partition.Value, result.Offset.Value);
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error publishing event {EventType} to topic {Topic}", typeof(TEvent).Name, topic);
                throw;
            }
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}

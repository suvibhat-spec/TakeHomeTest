using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Events;
using Microsoft.Extensions.Options;
using ECommerce.Shared.Kafka.Configuration;

namespace ECommerce.UserService.Kafka.Consumers
{
    public class OrderCreatedEventConsumer : KafkaConsumerService<OrderCreatedEvent>
    {
        public OrderCreatedEventConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumerService<OrderCreatedEvent>> logger,
            IOptions<KafkaTopicSettings> topicSettings)
            : base(kafkaSettings, serviceProvider, logger, topicSettings.Value.OrderCreated)
        {
            logger.LogInformation("[OrderCreatedEventConsumer] Initialized for topic {Topic}", topicSettings.Value.OrderCreated);
        }
    }
}

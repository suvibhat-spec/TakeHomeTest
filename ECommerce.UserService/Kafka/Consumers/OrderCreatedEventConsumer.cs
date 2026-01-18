using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Events;
using Microsoft.Extensions.Options;
using ECommerce.Shared.Kafka.Configuration;
using ECommerce.Shared.Kafka;

namespace ECommerce.UserService.Kafka.Consumers
{
    public class OrderCreatedEventConsumer : KafkaConsumerService<OrderCreatedEvent>
    {
        public OrderCreatedEventConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumerService<OrderCreatedEvent>> logger)
            : base(kafkaSettings, serviceProvider, logger, TopicConstants.OrderCreated)
        {
            logger.LogInformation("[OrderCreatedEventConsumer] Initialized for topic {Topic}", TopicConstants.OrderCreated);
        }
    }
}

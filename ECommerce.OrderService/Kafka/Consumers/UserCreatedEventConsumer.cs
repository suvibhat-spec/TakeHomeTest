using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Events;
using Microsoft.Extensions.Options;
using ECommerce.Shared.Kafka.Configuration;
using ECommerce.Shared.Kafka;

namespace ECommerce.OrderService.Kafka.Consumers
{
    public class UserCreatedEventConsumer : KafkaConsumerService<UserCreatedEvent>
    {
        public UserCreatedEventConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumerService<UserCreatedEvent>> logger)
            : base(kafkaSettings, serviceProvider, logger, TopicConstants.UserCreated)
        {
            logger.LogInformation("[UserCreatedEventConsumer] Initialized for topic {Topic}", TopicConstants.UserCreated);
        }
    }
}

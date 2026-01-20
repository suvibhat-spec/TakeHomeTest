using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Events;
using Microsoft.Extensions.Options;
using ECommerce.Shared.Kafka.Configuration;

namespace ECommerce.OrderService.Kafka.Consumers
{
    public class UserCreatedEventConsumer : KafkaConsumerService<UserCreatedEvent>
    {
        public UserCreatedEventConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumerService<UserCreatedEvent>> logger,
            IOptions<KafkaTopicSettings> topicSettings)
            : base(kafkaSettings, serviceProvider, logger, topicSettings.Value.UserCreated)
        {
            logger.LogInformation("[UserCreatedEventConsumer] Initialized for topic {Topic}", topicSettings.Value.UserCreated);
        }
    }
}

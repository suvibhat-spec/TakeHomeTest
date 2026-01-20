namespace ECommerce.Shared.Kafka.Configuration
{
    public class KafkaTopicSettings
    {
        public string OrderCreated { get; set; } = "order.created";
        public string UserCreated { get; set; } = "user.created";
    }
}

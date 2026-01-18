namespace ECommerce.Shared.Kafka.Configuration
{
    public class KafkaSettings
    {
        public string? BootstrapServers { get; set; }
        public ProducerSettings Producer { get; set; } = new();
        public ConsumerSettings Consumer { get; set; } = new();
    }

    public class ProducerSettings
    {
        public string? ClientId { get; set; }
        public int MessageTimeoutMs { get; set; } = 5000;
    }

    public class ConsumerSettings
    {
        public string? GroupId { get; set; }
        public string? AutoOffsetReset { get; set; } = "earliest";
        public bool EnableAutoCommit { get; set; } = false;
    }
}

namespace ECommerce.Shared.Kafka.Producer
{
    public interface IKafkaProducer
    {
        Task PublishAsync<TEvent>(string topic, string key, TEvent eventData);
    }
}
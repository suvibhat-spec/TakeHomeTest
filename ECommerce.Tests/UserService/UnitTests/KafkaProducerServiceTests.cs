using ECommerce.Shared.Kafka.Configuration;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.UnitTests.UserService;

public class KafkaProducerServiceTests
{
    [Fact]
    public void KafkaProducerService_Constructor_WithValidSettings_ShouldInitialize()
    {
        // Arrange
        var kafkaSettings = new KafkaSettings
        {
            BootstrapServers = "localhost:9092",
            Producer = new ProducerSettings
            {
                ClientId = "test-producer",
                MessageTimeoutMs = 5000
            }
        };
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<IKafkaProducer>>();
        var options = Options.Create(kafkaSettings);

        // Act
        var producer = new KafkaProducerService(options, mockLogger.Object);
        // Assert
        Assert.NotNull(producer);
    }

    [Fact]
    public void KafkaProducerService_Dispose_ShouldNotThrow()
    {
        // Arrange
        var kafkaSettings = new KafkaSettings
        {
            BootstrapServers = "localhost:9092",
            Producer = new ProducerSettings
            {
                ClientId = "test-producer",
                MessageTimeoutMs = 5000
            }
        };
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<IKafkaProducer>>();
        var options = Options.Create(kafkaSettings);
        var producer = new KafkaProducerService(options, mockLogger.Object);

        // Act & Assert
        producer.Dispose(); // Should not throw
    }
}

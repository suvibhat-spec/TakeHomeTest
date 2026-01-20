using AutoMapper;
using ECommerce.OrderService.Configuration;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Repositories;
using ECommerce.Shared.Kafka.Producer;
using ECommerce.Shared.Kafka.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrderSvc = ECommerce.OrderService.Service.OrderService;
using ECommerce.OrderService.Service;

namespace ECommerce.UnitTests.Orderservice;

public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<IOrderService>> _mockLogger;
        private readonly Mock<IKafkaProducer> _mockKafkaProducer;
        private readonly Mock<IOptions<KafkaTopicSettings>> _mockTopicSettings;
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<IOptions<ServiceUrlSettings>> _mockServiceUrlSettings;

        public OrderServiceTests()
        {
            _mockRepository = new Mock<IOrderRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<IOrderService>>();
            _mockKafkaProducer = new Mock<IKafkaProducer>();
            _mockTopicSettings = new Mock<IOptions<KafkaTopicSettings>>();
            _mockTopicSettings.Setup(x => x.Value).Returns(new KafkaTopicSettings());
            _mockHttpClient = new Mock<HttpClient>();
            _mockServiceUrlSettings = new Mock<IOptions<ServiceUrlSettings>>();
            _mockServiceUrlSettings.Setup(x => x.Value).Returns(new ServiceUrlSettings { UserService = "http://localhost:5001" });
        }    [Fact]
    public async Task GetOrderAsync_WithValidId_ShouldReturnOrderResponseDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var order = new Order { Id = orderId, UserId = userId, Product = "Laptop", Quantity = 1, Price = 999.99m };
        var responseDto = new OrderResponseDto { Id = orderId, UserId = userId, Product = "Laptop", Quantity = 1, Price = 999.99m };

        _mockRepository.Setup(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<OrderResponseDto>(order)).Returns(responseDto);

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object, _mockHttpClient.Object, _mockTopicSettings.Object, _mockServiceUrlSettings.Object);

        // Act
        var result = await service.GetOrderAsync(orderId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal("Laptop", result.Product);
        _mockRepository.Verify(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<OrderResponseDto>(order), Times.Once);
    }

    [Fact]
    public async Task GetOrderAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object, _mockHttpClient.Object, _mockTopicSettings.Object, _mockServiceUrlSettings.Object);

        // Act
        var result = await service.GetOrderAsync(orderId, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidDto_ShouldCreateAndReturnOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createDto = new CreateOrderRequestDto { UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };
        var createdOrder = new Order { Id = Guid.NewGuid(), UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };
        var responseDto = new OrderResponseDto { Id = createdOrder.Id, UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };

        _mockRepository.Setup(r => r.IsUserExistAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.CreateOrderAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdOrder);
        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(createdOrder);
        _mockMapper.Setup(m => m.Map<OrderResponseDto>(createdOrder)).Returns(responseDto);
        _mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object, _mockHttpClient.Object, _mockTopicSettings.Object, _mockServiceUrlSettings.Object);

        // Act
        var result = await service.CreateOrderAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Desktop PC", result.Product);
        _mockRepository.Verify(r => r.CreateOrderAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
        _mockKafkaProducer.Verify(k => k.PublishAsync(_mockTopicSettings.Object.Value.OrderCreated, createdOrder.Id.ToString(), It.IsAny<object>()), Times.Once);
    }
}

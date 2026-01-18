using AutoMapper;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Repositories;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.Extensions.Logging;
using Moq;
using OrderSvc = ECommerce.OrderService.Service.OrderService;
using ECommerce.OrderService.Service;
using ECommerce.Shared.Kafka;

namespace ECommerce.UnitTests.Orderservice;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<IOrderService>> _mockLogger;
    private readonly Mock<IKafkaProducer> _mockKafkaProducer;

    public OrderServiceTests()
    {
        _mockRepository = new Mock<IOrderRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<IOrderService>>();
        _mockKafkaProducer = new Mock<IKafkaProducer>();
    }

    [Fact]
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

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

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

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

        // Act
        var result = await service.GetOrderAsync(orderId, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnAllOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Product = "Monitor", Quantity = 1, Price = 300m },
            new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Product = "Keyboard", Quantity = 2, Price = 90m }
        };

        var dtos = orders.Select(o => new OrderResponseDto { Id = o.Id, UserId = o.UserId, Product = o.Product, Quantity = o.Quantity, Price = o.Price }).ToList();

        _mockRepository.Setup(r => r.GetAllOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _mockMapper.Setup(m => m.Map<IEnumerable<OrderResponseDto>>(orders)).Returns(dtos);

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

        // Act
        var result = await service.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.GetAllOrdersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidDto_ShouldCreateAndReturnOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createDto = new CreateOrderRequestDto { UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };
        var createdOrder = new Order { Id = Guid.NewGuid(), UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };
        var responseDto = new OrderResponseDto { Id = createdOrder.Id, UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };

        _mockRepository.Setup(r => r.CreateOrderAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdOrder);
        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(createdOrder);
        _mockMapper.Setup(m => m.Map<OrderResponseDto>(createdOrder)).Returns(responseDto);
        _mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

        // Act
        var result = await service.CreateOrderAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Desktop PC", result.Product);
        _mockRepository.Verify(r => r.CreateOrderAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
        _mockKafkaProducer.Verify(k => k.PublishAsync(TopicConstants.OrderCreated, createdOrder.Id.ToString(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        _mockMapper.Setup(m => m.Map<IEnumerable<OrderResponseDto>>(It.IsAny<IEnumerable<Order>>()))
            .Returns(new List<OrderResponseDto>());

        var service = new OrderSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

        // Act
        var result = await service.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

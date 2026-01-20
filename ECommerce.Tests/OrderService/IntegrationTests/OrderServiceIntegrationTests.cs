using AutoMapper;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Data;
using ECommerce.OrderService.Repositories;
using ECommerce.OrderService.Service;
using ECommerce.OrderService.Models;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ECommerce.Shared.Kafka.Configuration;
using Moq;
using OrderSvc = ECommerce.OrderService.Service.OrderService;

namespace ECommerce.Tests.OrderService.IntegrationTests;

/// <summary>
/// Integration tests for OrderService with real database and mocked Kafka
/// </summary>
public class OrderServiceIntegrationTests
{
    private OrderDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    private IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Order, OrderResponseDto>();
            cfg.CreateMap<CreateOrderRequestDto, Order>();
        });
        return config.CreateMapper();
    }

    private IOptions<KafkaTopicSettings> GetMockTopicSettings()
    {
        var mockTopicSettings = new Mock<IOptions<KafkaTopicSettings>>();
        mockTopicSettings.Setup(x => x.Value).Returns(new KafkaTopicSettings());
        return mockTopicSettings.Object;
    }

    private void SeedUserReference(OrderDbContext context, Guid userId)
    {
        context.UserRefs.Add(new UserRef { Id = userId });
        context.SaveChanges();
    }

    [Fact]
    public async Task GetOrderAsync_WithValidId_ShouldReturnOrderFromDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var orderId = Guid.NewGuid();
        var order = new Order 
        { 
            Id = orderId, 
            UserId = userId, 
            Product = "Laptop", 
            Quantity = 1, 
            Price = 999.99m 
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act
        var result = await service.GetOrderAsync(orderId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal("Laptop", result.Product);
        Assert.Equal(1, result.Quantity);
        Assert.Equal(999.99m, result.Price);
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithMultipleOrders_ShouldReturnAllFromDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        SeedUserReference(context, userId1);
        SeedUserReference(context, userId2);

        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = userId1, Product = "Monitor", Quantity = 2, Price = 300m },
            new Order { Id = Guid.NewGuid(), UserId = userId2, Product = "Keyboard", Quantity = 1, Price = 150m },
            new Order { Id = Guid.NewGuid(), UserId = userId1, Product = "Mouse", Quantity = 3, Price = 75m }
        };
        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act
        var result = await service.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.All(result, dto => Assert.NotNull(dto.Product));
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidData_ShouldPersistToDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDto = new CreateOrderRequestDto 
        { 
            UserId = userId, 
            Product = "Desktop PC", 
            Quantity = 1, 
            Price = 1500.00m 
        };

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act
        var result = await service.CreateOrderAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Desktop PC", result.Product);
        Assert.Equal(1, result.Quantity);
        Assert.Equal(1500.00m, result.Price);

        // Verify persisted to database
        var orderInDb = await context.Orders.FirstOrDefaultAsync(o => o.Id == result.Id);
        Assert.NotNull(orderInDb);
        Assert.Equal("Desktop PC", orderInDb.Product);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldPublishKafkaEvent()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDto = new CreateOrderRequestDto 
        { 
            UserId = userId, 
            Product = "Headphones", 
            Quantity = 2, 
            Price = 200m 
        };

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act
        var result = await service.CreateOrderAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockKafkaProducer.Verify(
            k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), 
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenKafkaFails_ShouldStillPersistOrder()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDto = new CreateOrderRequestDto 
        { 
            UserId = userId, 
            Product = "Speaker", 
            Quantity = 1, 
            Price = 250m 
        };

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("Kafka error"));
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act & Assert - Should not throw even if Kafka fails
        var result = await service.CreateOrderAsync(createDto, CancellationToken.None);

        // Verify order still persisted despite Kafka failure
        Assert.NotNull(result);
        var orderInDb = await context.Orders.FirstOrDefaultAsync(o => o.Id == result.Id);
        Assert.NotNull(orderInDb);
        Assert.Equal("Speaker", orderInDb.Product);
    }

    [Fact]
    public async Task GetOrderAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act
        var result = await service.GetOrderAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act
        var result = await service.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateOrderAsync_MultipleOrders_AllShouldPersist()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDtos = new List<CreateOrderRequestDto>
        {
            new CreateOrderRequestDto { UserId = userId, Product = "Product A", Quantity = 1, Price = 100m },
            new CreateOrderRequestDto { UserId = userId, Product = "Product B", Quantity = 2, Price = 200m },
            new CreateOrderRequestDto { UserId = userId, Product = "Product C", Quantity = 3, Price = 300m }
        };

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IOrderService>>();
        var service = new OrderSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object, GetMockTopicSettings());

        // Act
        foreach (var dto in createDtos)
        {
            await service.CreateOrderAsync(dto, CancellationToken.None);
        }

        // Assert - Verify all orders persisted
        var allOrders = await context.Orders.ToListAsync();
        Assert.Equal(3, allOrders.Count);
        Assert.Contains(allOrders, o => o.Product == "Product A");
        Assert.Contains(allOrders, o => o.Product == "Product B");
        Assert.Contains(allOrders, o => o.Product == "Product C");
    }
}

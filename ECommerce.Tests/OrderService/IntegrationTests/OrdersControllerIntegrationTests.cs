using AutoMapper;
using ECommerce.OrderService.Configuration;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Data;
using ECommerce.OrderService.Controller;
using ECommerce.OrderService.Repositories;
using ECommerce.OrderService.Service;
using ECommerce.OrderService.Models;
using ECommerce.Shared.Kafka.Producer;
using ECommerce.Shared.Kafka.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.Tests.OrderService.IntegrationTests;

/// <summary>
/// Integration tests for OrdersController with real service layer and database
/// </summary>
public class OrdersControllerIntegrationTests
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

    private void SeedUserReference(OrderDbContext context, Guid userId)
    {
        context.UserRefs.Add(new UserRef { Id = userId });
        context.SaveChanges();
    }

    private OrdersController GetController(OrderDbContext context)
    {        
        var mockLogger = new Mock<ILogger<OrdersController>>();
        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var mapper = GetMapper();
        
        // Create OrderService with required mocks
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockServiceLogger = new Mock<ILogger<IOrderService>>();
        var mockHttpClient = new Mock<HttpClient>();
        var mockTopicSettings = new Mock<IOptions<KafkaTopicSettings>>();
        mockTopicSettings.Setup(x => x.Value).Returns(new KafkaTopicSettings());
        var mockServiceUrlSettings = new Mock<IOptions<ServiceUrlSettings>>();
        mockServiceUrlSettings.Setup(x => x.Value).Returns(new ServiceUrlSettings { UserService = "http://localhost:5001" });
        
        var orderService = new ECommerce.OrderService.Service.OrderService(
            repository, 
            mapper, 
            mockKafkaProducer.Object, 
            mockServiceLogger.Object,
            mockHttpClient.Object,
            mockTopicSettings.Object,
            mockServiceUrlSettings.Object
        );
        
        return new OrdersController(orderService, mapper, mockLogger.Object);
    }

    [Fact]
    public async Task GetOrder_WithValidId_ShouldReturnOkWithOrder()
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

        var controller = GetController(context);

        // Act
        var result = await controller.GetOrder(orderId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        var orderDto = Assert.IsType<OrderResponseDto>(okResult.Value);
        Assert.Equal(orderId, orderDto.Id);
        Assert.Equal("Laptop", orderDto.Product);
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.GetOrder(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task AddOrder_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDto = new CreateOrderRequestDto 
        { 
            UserId = userId, 
            Product = "Monitor", 
            Quantity = 1, 
            Price = 500m 
        };
        var controller = GetController(context);

        // Act
        var result = await controller.AddOrder(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(controller.GetOrder), createdResult.ActionName);
        var orderDto = Assert.IsType<OrderResponseDto>(createdResult.Value);
        Assert.Equal("Monitor", orderDto.Product);
        Assert.Equal(1, orderDto.Quantity);
    }

    [Fact]
    public async Task AddOrder_ShouldPersistOrderToDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDto = new CreateOrderRequestDto 
        { 
            UserId = userId, 
            Product = "Keyboard", 
            Quantity = 2, 
            Price = 150m 
        };
        var controller = GetController(context);

        // Act
        var result = await controller.AddOrder(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var orderDto = Assert.IsType<OrderResponseDto>(createdResult.Value);

        // Verify in database
        var orderInDb = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderDto.Id);
        Assert.NotNull(orderInDb);
        Assert.Equal("Keyboard", orderInDb.Product);
        Assert.Equal(2, orderInDb.Quantity);
    }

    [Fact]
    public async Task GetOrder_AfterCreate_ShouldReturnSameData()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDto = new CreateOrderRequestDto 
        { 
            UserId = userId, 
            Product = "Mouse", 
            Quantity = 3, 
            Price = 75m 
        };
        var controller = GetController(context);

        // Act - Create order
        var createResult = await controller.AddOrder(createDto, CancellationToken.None);
        var createdResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdOrderDto = Assert.IsType<OrderResponseDto>(createdResult.Value);

        // Act - Get order
        var getResult = await controller.GetOrder(createdOrderDto.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
        var retrievedOrderDto = Assert.IsType<OrderResponseDto>(okResult.Value);
        Assert.Equal(createdOrderDto.Id, retrievedOrderDto.Id);
        Assert.Equal(createdOrderDto.Product, retrievedOrderDto.Product);
        Assert.Equal(createdOrderDto.Quantity, retrievedOrderDto.Quantity);
        Assert.Equal(createdOrderDto.Price, retrievedOrderDto.Price);
    }

    [Fact]
    public async Task AddMultipleOrders_AllShouldBePersisted()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        SeedUserReference(context, userId);

        var createDtos = new List<CreateOrderRequestDto>
        {
            new CreateOrderRequestDto { UserId = userId, Product = "Item A", Quantity = 1, Price = 100m },
            new CreateOrderRequestDto { UserId = userId, Product = "Item B", Quantity = 2, Price = 200m },
            new CreateOrderRequestDto { UserId = userId, Product = "Item C", Quantity = 3, Price = 300m }
        };
        var controller = GetController(context);

        // Act
        foreach (var dto in createDtos)
        {
            await controller.AddOrder(dto, CancellationToken.None);
        }

        // Assert - Verify all orders exist in database
        var allOrders = await context.Orders.ToListAsync();
        Assert.Equal(3, allOrders.Count);
        Assert.Contains(allOrders, o => o.Product == "Item A");
        Assert.Contains(allOrders, o => o.Product == "Item B");
        Assert.Contains(allOrders, o => o.Product == "Item C");
    }
}

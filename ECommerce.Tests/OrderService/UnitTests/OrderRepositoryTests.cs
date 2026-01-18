using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Models;
using ECommerce.OrderService.Data;
using ECommerce.OrderService.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging;

namespace ECommerce.UnitTests.Orderservice;

public class OrderRepositoryTests
{
    private OrderDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    [Fact]
    public async Task GetOrderAsync_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var orderId = Guid.NewGuid();
        var order = new Order 
        { 
            Id = orderId, 
            UserId = Guid.NewGuid(), 
            Product = "Laptop", 
            Quantity = 1, 
            Price = 999.99m 
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        // Act
        var result = await repository.GetOrderAsync(orderId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal("Laptop", result.Product);
        Assert.Equal(1, result.Quantity);
    }

    [Fact]
    public async Task GetOrderAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);

        // Act
        var result = await repository.GetOrderAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidData_ShouldCreateOrder()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        
        // Add user reference to satisfy foreign key constraint
        var userRef = new UserRef { Id = userId };
        context.UserRefs.Add(userRef);
        await context.SaveChangesAsync();
        
        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var createDto = new CreateOrderRequestDto 
        { 
            UserId = userId, 
            Product = "Desktop PC", 
            Quantity = 2, 
            Price = 1500.00m 
        };

        // Act
        var result = await repository.CreateOrderAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Desktop PC", result.Product);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(1500.00m, result.Price);
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithMultipleOrders_ShouldReturnAllOrders()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Product = "Monitor", Quantity = 1, Price = 300m },
            new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Product = "Keyboard", Quantity = 3, Price = 90m },
            new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Product = "Mouse", Quantity = 5, Price = 50m }
        };
        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);

        // Act
        var result = await repository.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithNoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);

        // Act
        var result = await repository.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithMultipleCreations_ShouldReturnAllCreatedOrders()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        // Add user references
        context.UserRefs.AddRange(
            new UserRef { Id = userId1 },
            new UserRef { Id = userId2 }
        );
        await context.SaveChangesAsync();
        
        var repository = new OrderRepository(context, new Mock<ILogger<OrderRepository>>().Object);
        var createDto1 = new CreateOrderRequestDto { UserId = userId1, Product = "Headphones", Quantity = 2, Price = 150m };
        var createDto2 = new CreateOrderRequestDto { UserId = userId2, Product = "Speaker", Quantity = 1, Price = 200m };

        // Act
        await repository.CreateOrderAsync(createDto1, CancellationToken.None);
        await repository.CreateOrderAsync(createDto2, CancellationToken.None);
        var result = await repository.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
    }
}

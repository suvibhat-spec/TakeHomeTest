using AutoMapper;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Repositories;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.Extensions.Logging;
using Moq;
using OrderSvc = ECommerce.OrderService.Service.OrderService;
using ECommerce.OrderService.Service;

namespace ECommerce.UnitTests.Orderservice;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ILogger<OrdersControllerTests>> _mockLogger;

    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockLogger = new Mock<ILogger<OrdersControllerTests>>();
    }

    [Fact]
    public async Task GetOrder_WithValidId_ShouldReturnOrderResponseDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var orderDto = new OrderResponseDto { Id = orderId, UserId = userId, Product = "Laptop", Quantity = 1, Price = 999.99m };

        _mockOrderService.Setup(s => s.GetOrderAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderDto);

        // Act
        var result = await _mockOrderService.Object.GetOrderAsync(orderId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal("Laptop", result.Product);
        _mockOrderService.Verify(s => s.GetOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderService.Setup(s => s.GetOrderAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderResponseDto?)null);

        // Act
        var result = await _mockOrderService.Object.GetOrderAsync(orderId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldCreateOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createDto = new CreateOrderRequestDto { UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };
        var createdOrderDto = new OrderResponseDto { Id = Guid.NewGuid(), UserId = userId, Product = "Desktop PC", Quantity = 1, Price = 1500m };

        _mockOrderService.Setup(s => s.CreateOrderAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdOrderDto);

        // Act
        var result = await _mockOrderService.Object.CreateOrderAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Desktop PC", result.Product);
        Assert.Equal(1500m, result.Price);
        _mockOrderService.Verify(s => s.CreateOrderAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidUserId_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createDto = new CreateOrderRequestDto { UserId = userId, Product = "Invalid Product", Quantity = 1, Price = 100m };

        _mockOrderService.Setup(s => s.CreateOrderAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("User does not exist"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _mockOrderService.Object.CreateOrderAsync(createDto, CancellationToken.None));
    }
}

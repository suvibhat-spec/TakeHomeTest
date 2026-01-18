using ECommerce.UserService.Controller;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.UnitTests.UserService;

public class UsersControllerTests
{
    [Fact]
    public async Task GetUser_WithValidId_ShouldReturnOkWithUser()
    {
        // Arrange
        var mockService = new Mock<IUserService>();
        var mockLogger = new Mock<ILogger<UsersController>>();
        
        var userId = Guid.NewGuid();
        var userDto = new UserResponseDto { Id = userId, Name = "Test User", Email = "test@example.com" };

        mockService.Setup(s => s.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var controller = new UsersController(mockService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetUser(userId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        mockService.Verify(s => s.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var mockService = new Mock<IUserService>();
        var mockLogger = new Mock<ILogger<UsersController>>();
        
        var userId = Guid.NewGuid();
        mockService.Setup(s => s.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponseDto?)null);

        var controller = new UsersController(mockService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetUser(userId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(userId, notFoundResult.Value);
    }

    [Fact]
    public async Task AddUser_WithValidDto_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<IUserService>();
        var mockLogger = new Mock<ILogger<UsersController>>();
        
        var createDto = new CreateUserRequestDto { Name = "New User", Email = "new@example.com" };
        var responseDto = new UserResponseDto { Id = Guid.NewGuid(), Name = "New User", Email = "new@example.com" };

        mockService.Setup(s => s.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = new UsersController(mockService.Object, mockLogger.Object);

        // Act
        var result = await controller.AddUser(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(UsersController.GetUser), createdResult.ActionName);
        mockService.Verify(s => s.CreateUserAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }
}

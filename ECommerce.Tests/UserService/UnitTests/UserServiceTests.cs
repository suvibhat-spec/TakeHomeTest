using AutoMapper;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;
using ECommerce.UserService.Repositories;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.Extensions.Logging;
using Moq;
using UserSvc = ECommerce.UserService.Services.UserService;
using ECommerce.UserService.Services;
using ECommerce.Shared.Kafka;

namespace ECommerce.UnitTests.UserService;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<IUserService>> _mockLogger;
    private readonly Mock<IKafkaProducer> _mockKafkaProducer;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<IUserService>>();
        _mockKafkaProducer = new Mock<IKafkaProducer>();
    }

    [Fact]
    public async Task GetUserAsync_WithValidId_ShouldReturnUserResponseDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
        var responseDto = new UserResponseDto { Id = userId, Name = "Test User", Email = "test@example.com" };

        _mockRepository.Setup(r => r.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserResponseDto>(user)).Returns(responseDto);

        var service = new UserSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

        // Act
        var result = await service.GetUserAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Test User", result.Name);
        _mockRepository.Verify(r => r.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<UserResponseDto>(user), Times.Once);
    }

    [Fact]
    public async Task GetUserAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

        // Act
        var result = await service.GetUserAsync(userId, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidDto_ShouldCreateAndReturnUser()
    {
        // Arrange
        var createDto = new CreateUserRequestDto { Name = "New User", Email = "new@example.com" };
        var createdUser = new User { Id = Guid.NewGuid(), Name = "New User", Email = "new@example.com" };
        var responseDto = new UserResponseDto { Id = createdUser.Id, Name = "New User", Email = "new@example.com" };

        _mockRepository.Setup(r => r.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);
        _mockMapper.Setup(m => m.Map<User>(createDto)).Returns(createdUser);
        _mockMapper.Setup(m => m.Map<UserResponseDto>(createdUser)).Returns(responseDto);
        _mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var service = new UserSvc(_mockRepository.Object, _mockMapper.Object, _mockKafkaProducer.Object, _mockLogger.Object);

        // Act
        var result = await service.CreateUserAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New User", result.Name);
        _mockRepository.Verify(r => r.CreateUserAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
        _mockKafkaProducer.Verify(k => k.PublishAsync(TopicConstants.UserCreated, createdUser.Id.ToString(), It.IsAny<object>()), Times.Once);
    }
}

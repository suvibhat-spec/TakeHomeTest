using AutoMapper;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;
using ECommerce.UserService.Data;
using ECommerce.UserService.Repositories;
using ECommerce.UserService.Services;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UserSvc = ECommerce.UserService.Services.UserService;
using ECommerce.Shared.Kafka;

namespace ECommerce.Tests.UserService.IntegrationTests;

/// <summary>
/// Integration tests for UserService with real database and mocked Kafka
/// </summary>
public class UserServiceIntegrationTests
{
    private UserDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UserDbContext(options);
    }

    private IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserResponseDto>();
            cfg.CreateMap<CreateUserRequestDto, User>();
        });
        return config.CreateMapper();
    }

    [Fact]
    public async Task GetUserAsync_WithValidId_ShouldReturnUserFromDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IUserService>>();
        var service = new UserSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object);

        // Act
        var result = await service.GetUserAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldPersistToDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var createDto = new CreateUserRequestDto { Name = "New User", Email = "newuser@example.com" };

        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<IUserService>>();
        var service = new UserSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object);

        // Act
        var result = await service.CreateUserAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New User", result.Name);
        Assert.Equal("newuser@example.com", result.Email);

        // Verify persistence in database
        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        Assert.NotNull(userInDb);
        Assert.Equal("New User", userInDb.Name);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldPublishKafkaEvent()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var createDto = new CreateUserRequestDto { Name = "Test User", Email = "test@example.com" };

        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<IUserService>>();
        var service = new UserSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object);

        // Act
        var result = await service.CreateUserAsync(createDto, CancellationToken.None);

        // Assert
        mockKafkaProducer.Verify(
            k => k.PublishAsync(TopicConstants.UserCreated, result.Id.ToString(), It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WhenKafkaFails_ShouldStillPersistUser()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var createDto = new CreateUserRequestDto { Name = "Resilient User", Email = "resilient@example.com" };

        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("Kafka connection failed"));
        var mockLogger = new Mock<ILogger<IUserService>>();
        var service = new UserSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object);

        // Act
        var result = await service.CreateUserAsync(createDto, CancellationToken.None);

        // Assert - User should still be created even if Kafka fails
        Assert.NotNull(result);
        Assert.Equal("Resilient User", result.Name);

        // Verify persistence in database
        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == "resilient@example.com");
        Assert.NotNull(userInDb);
    }

    [Fact]
    public async Task GetUserAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        var mockLogger = new Mock<ILogger<IUserService>>();
        var service = new UserSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object);

        // Act
        var result = await service.GetUserAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task MultipleCreateOperations_ShouldAllPersistCorrectly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<IUserService>>();
        var service = new UserSvc(repository, mapper, mockKafkaProducer.Object, mockLogger.Object);

        var dtos = new List<CreateUserRequestDto>
        {
            new CreateUserRequestDto { Name = "Alice", Email = "alice@example.com" },
            new CreateUserRequestDto { Name = "Bob", Email = "bob@example.com" },
            new CreateUserRequestDto { Name = "Charlie", Email = "charlie@example.com" }
        };

        // Act
        var results = new List<UserResponseDto>();
        foreach (var dto in dtos)
        {
            results.Add(await service.CreateUserAsync(dto, CancellationToken.None));
        }

        // Assert
        Assert.Equal(3, results.Count);

        var user1 = await service.GetUserAsync(results[0].Id, CancellationToken.None);
        var user2 = await service.GetUserAsync(results[1].Id, CancellationToken.None);
        var user3 = await service.GetUserAsync(results[2].Id, CancellationToken.None);
        Assert.NotNull(user1);
        Assert.NotNull(user2);
        Assert.NotNull(user3);
    }
}

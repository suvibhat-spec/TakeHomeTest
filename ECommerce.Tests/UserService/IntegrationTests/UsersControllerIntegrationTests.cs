using AutoMapper;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;
using ECommerce.UserService.Data;
using ECommerce.UserService.Controller;
using ECommerce.UserService.Repositories;
using ECommerce.UserService.Services;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ECommerce.Shared.Kafka.Configuration;
using Moq;
using UserSvc = ECommerce.UserService.Services.UserService;

namespace ECommerce.Tests.UserService.IntegrationTests;

/// <summary>
/// Integration tests for UsersController with real service layer and database
/// </summary>
public class UsersControllerIntegrationTests
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

    private IOptions<KafkaTopicSettings> GetMockTopicSettings()
    {
        var mockTopicSettings = new Mock<IOptions<KafkaTopicSettings>>();
        mockTopicSettings.Setup(x => x.Value).Returns(new KafkaTopicSettings());
        return mockTopicSettings.Object;
    }

    private UsersController GetController(UserDbContext context)
    {
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var mapper = GetMapper();
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<UsersController>>();
        var service = new UserSvc(repository, mapper, mockKafkaProducer.Object, new Mock<ILogger<IUserService>>().Object, GetMockTopicSettings());
        return new UsersController(service, mockLogger.Object);
    }

    [Fact]
    public async Task GetUser_WithValidId_ShouldReturnOkWithUser()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetUser(userId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        var userDto = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(userId, userDto.Id);
        Assert.Equal("Test User", userDto.Name);
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.GetUser(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task AddUser_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var createDto = new CreateUserRequestDto { Name = "New User", Email = "newuser@example.com" };
        var controller = GetController(context);

        // Act
        var result = await controller.AddUser(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(controller.GetUser), createdResult.ActionName);
        var userDto = Assert.IsType<UserResponseDto>(createdResult.Value);
        Assert.Equal("New User", userDto.Name);
        Assert.Equal("newuser@example.com", userDto.Email);
    }

    [Fact]
    public async Task AddUser_ShouldPersistUserToDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var createDto = new CreateUserRequestDto { Name = "Database Test", Email = "dbtest@example.com" };
        var controller = GetController(context);

        // Act
        var result = await controller.AddUser(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var userDto = Assert.IsType<UserResponseDto>(createdResult.Value);

        // Verify in database
        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Id == userDto.Id);
        Assert.NotNull(userInDb);
        Assert.Equal("Database Test", userInDb.Name);
        Assert.Equal("dbtest@example.com", userInDb.Email);
    }

    [Fact]
    public async Task AddMultipleUsers_AllShouldBePersisted()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = GetController(context);
        var createDtos = new List<CreateUserRequestDto>
        {
            new CreateUserRequestDto { Name = "User A", Email = "usera@example.com" },
            new CreateUserRequestDto { Name = "User B", Email = "userb@example.com" },
            new CreateUserRequestDto { Name = "User C", Email = "userc@example.com" }
        };

        // Act
        foreach (var dto in createDtos)
        {
            await controller.AddUser(dto, CancellationToken.None);
        }

        // Assert - Verify all users exist in database
        var allUsers = await context.Users.ToListAsync();
        Assert.Equal(3, allUsers.Count);
        Assert.Contains(allUsers, u => u.Name == "User A");
        Assert.Contains(allUsers, u => u.Name == "User B");
        Assert.Contains(allUsers, u => u.Name == "User C");
    }

    [Fact]
    public async Task GetUser_AfterCreate_ShouldReturnSameData()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var createDto = new CreateUserRequestDto { Name = "Integration Test", Email = "integration@example.com" };
        var controller = GetController(context);

        // Act - Create user
        var createResult = await controller.AddUser(createDto, CancellationToken.None);
        var createdResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdUserDto = Assert.IsType<UserResponseDto>(createdResult.Value);

        // Act - Get user
        var getResult = await controller.GetUser(createdUserDto.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
        var retrievedUserDto = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(createdUserDto.Id, retrievedUserDto.Id);
        Assert.Equal(createdUserDto.Name, retrievedUserDto.Name);
        Assert.Equal(createdUserDto.Email, retrievedUserDto.Email);
    }
}

using ECommerce.UserService.Data;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;
using ECommerce.UserService.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.UnitTests.UserService;

public class UserRepositoryTests
{
    private UserDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new UserDbContext(options);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidDto_ShouldReturnCreatedUser()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var createDto = new CreateUserRequestDto { Name = "John Doe", Email = "john@example.com" };

        // Act
        var result = await repository.CreateUserAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Jane Doe", Email = "jane@example.com" };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetUserAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Jane Doe", result.Name);
    }

    [Fact]
    public async Task GetUserAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var invalidId = Guid.NewGuid();

        // Act
        var result = await repository.GetUserAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithMultipleUsers_ShouldReturnAllUsers()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "User 1", Email = "user1@example.com" },
            new User { Id = Guid.NewGuid(), Name = "User 2", Email = "user2@example.com" },
            new User { Id = Guid.NewGuid(), Name = "User 3", Email = "user3@example.com" }
        };
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllUsersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllUsersAsync_WithNoUsers_ShouldReturnEmptyList()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);

        // Act
        var result = await repository.GetAllUsersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldPersistUserInDatabase()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var createDto = new CreateUserRequestDto { Name = "Test User", Email = "test@example.com" };

        // Act
        var createdUser = await repository.CreateUserAsync(createDto, CancellationToken.None);
        var retrievedUser = await repository.GetUserAsync(createdUser.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Equal(createdUser.Id, retrievedUser.Id);
        Assert.Equal(createdUser.Name, retrievedUser.Name);
        Assert.Equal(createdUser.Email, retrievedUser.Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithMultipleCreations_ShouldReturnAllCreatedUsers()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, new Mock<ILogger<UserRepository>>().Object);
        var dtos = new List<CreateUserRequestDto>
        {
            new CreateUserRequestDto { Name = "Alice", Email = "alice@example.com" },
            new CreateUserRequestDto { Name = "Bob", Email = "bob@example.com" },
            new CreateUserRequestDto { Name = "Charlie", Email = "charlie@example.com" }
        };

        // Act
        foreach (var dto in dtos)
        {
            await repository.CreateUserAsync(dto, CancellationToken.None);
        }
        var allUsers = await repository.GetAllUsersAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, allUsers.Count());
        Assert.Contains(allUsers, u => u.Name == "Alice");
        Assert.Contains(allUsers, u => u.Name == "Bob");
        Assert.Contains(allUsers, u => u.Name == "Charlie");
    }
}

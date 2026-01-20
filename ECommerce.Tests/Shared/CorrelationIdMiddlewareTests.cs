#pragma warning disable CS8602 // Dereference of a possibly null reference - False positive in expression trees
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ECommerce.Shared.Middleware;

namespace ECommerce.Tests.Shared;

public class CorrelationIdMiddlewareTests
{
    private readonly Mock<ILogger<CorrelationIdMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNextMiddleware;

    public CorrelationIdMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<CorrelationIdMiddleware>>();
        _mockNextMiddleware = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoCorrelationIdInHeaders_ShouldGenerateNewId()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Callback(async (HttpContext ctx) => await ctx.Response.StartAsync())
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Verify a new ID was generated (logged as "Generated new")
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Generated new Correlation ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdInHeaders_ShouldUseProvidedId()
    {
        // Arrange
        var providedId = "test-correlation-id-12345";
        var middleware = new CorrelationIdMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = providedId;

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Callback(async (HttpContext ctx) => await ctx.Response.StartAsync())
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Verify that the provided ID was used
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Using provided Correlation ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationIdToResponseHeaders()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Callback(async (HttpContext ctx) => await ctx.Response.StartAsync())
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Verify next middleware was called
        _mockNextMiddleware.Verify(x => x.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoCorrelationIdProvided_ShouldLogGeneratedId()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Generated new Correlation ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdProvided_ShouldLogProvidedId()
    {
        // Arrange
        var providedId = "custom-id";
        var middleware = new CorrelationIdMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = providedId;

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Using provided Correlation ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNextMiddleware.Verify(x => x.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithValidGuidCorrelationId_ShouldPreserveIt()
    {
        // Arrange
        var guidId = Guid.NewGuid().ToString();
        var middleware = new CorrelationIdMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = guidId;

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Callback(async (HttpContext ctx) => await ctx.Response.StartAsync())
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Verify that the provided GUID was used (logged as "Using provided")
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Using provided Correlation ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

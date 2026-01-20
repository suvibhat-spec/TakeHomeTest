#pragma warning disable CS8602 // Dereference of a possibly null reference - False positive in expression trees
using ECommerce.Shared.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Shared;

public class ExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNextMiddleware;

    public ExceptionHandlerMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _mockNextMiddleware = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoExceptionThrown_ShouldCallNextMiddleware()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();

        _mockNextMiddleware.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNextMiddleware.Verify(x => x.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentExceptionThrown_ShouldReturn404()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        var responseBody = new System.IO.MemoryStream();
        context.Response.Body = responseBody;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Throws(new ArgumentException("Invalid argument"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenEmailTakenExceptionThrown_ShouldReturn409()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        var responseBody = new System.IO.MemoryStream();
        context.Response.Body = responseBody;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Throws(new InvalidOperationException("Email already taken"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationExceptionThrown_ShouldReturn400()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        var responseBody = new System.IO.MemoryStream();
        context.Response.Body = responseBody;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Throws(new InvalidOperationException("Invalid operation"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenGenericExceptionThrown_ShouldReturn500()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        var responseBody = new System.IO.MemoryStream();
        context.Response.Body = responseBody;

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Throws(new Exception("Generic error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var middleware = new ExceptionHandlingMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Throws(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unhandled exception occurred")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldSetJsonContentType()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_mockNextMiddleware.Object, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();

        _mockNextMiddleware
            .Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Throws(new Exception("Test"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("application/json", context.Response.ContentType);
    }
}

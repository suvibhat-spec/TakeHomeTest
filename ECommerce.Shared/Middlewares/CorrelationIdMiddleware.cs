using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ECommerce.Shared.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;
        if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationIdHeader))
        {
            correlationId = Guid.NewGuid().ToString();
            logger.LogInformation("Generated new Correlation ID: {CorrelationId}", correlationId);
        }
        else
        {
            correlationId = correlationIdHeader.ToString();
            logger.LogInformation("Using provided Correlation ID: {CorrelationId}", correlationId);
        }

        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId.ToString();
                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}

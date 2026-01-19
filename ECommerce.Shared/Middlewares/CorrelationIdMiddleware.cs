using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ECommerce.Shared.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            logger.LogInformation("Generated new Correlation ID: {CorrelationId}", correlationId);
        }
        else
        {
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

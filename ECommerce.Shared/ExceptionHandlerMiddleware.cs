using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    //private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next//, 
        //ILogger<ExceptionHandlingMiddleware> logger
        )
    {
        _next = next;
       // _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        /*var response = exception switch
        {
            NotFoundException notFound => new
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = notFound.Message,
                Details = notFound.Details
            },
            ValidationException validation => new
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = validation.Message,
                Errors = validation.Errors
            },
            UnauthorizedException => new
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Unauthorized access"
            },
            _ => new
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An internal server error occurred"
            }
        };
*/
        //context.Response.StatusCode = response.StatusCode;
        //await context.Response.WriteAsJsonAsync(response);
    }
}

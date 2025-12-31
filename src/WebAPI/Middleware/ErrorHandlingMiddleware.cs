using Microsoft.AspNetCore.Mvc;
using Sqordia.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace WebAPI.Middleware;

// TODO: Global error handling
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;

        switch (exception)
        {
            case BusinessRuleValidationException validationException:
                code = HttpStatusCode.BadRequest;
                result = JsonSerializer.Serialize(new
                {
                    errors = validationException.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
                break;
            case DomainException domainException:
                code = HttpStatusCode.BadRequest;
                result = JsonSerializer.Serialize(new { error = domainException.Message });
                break;
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                result = JsonSerializer.Serialize(new { error = "Unauthorized access" });
                break;
            case KeyNotFoundException:
                code = HttpStatusCode.NotFound;
                result = JsonSerializer.Serialize(new { error = "Resource not found" });
                break;
            default:
                _logger.LogError(exception, "An unhandled exception occurred");
                
                // Provide more specific error messages for common issues
                string errorMessage = exception switch
                {
                    InvalidOperationException when exception.Message.Contains("connection string") => 
                        "Database configuration error. Please check connection string configuration.",
                    InvalidOperationException when exception.Message.Contains("JWT") => 
                        "Authentication configuration error. Please check JWT settings.",
                    InvalidOperationException when exception.Message.Contains("Email") || exception.Message.Contains("SQS") => 
                        "Email service configuration error. Please check EMAIL_QUEUE_URL environment variable.",
                    _ => "An unexpected error occurred"
                };
                
                result = JsonSerializer.Serialize(new { error = errorMessage });
                break;
        }

        // Only set response properties if response hasn't started
        if (!context.Response.HasStarted)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            await context.Response.WriteAsync(result);
        }
        else
        {
            // If response has started, just log the error
            _logger.LogError(exception, "Exception occurred after response started");
        }
    }
}

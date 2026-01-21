using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SendellVoice.Web.Middleware;

/// <summary>
/// Global exception handler for the API.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation error"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Invalid operation"),
            NotSupportedException => (StatusCodes.Status400BadRequest, "Operation not supported"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred while processing your request")
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() ? exception.Message : null,
            Instance = httpContext.Request.Path
        };

        if (_environment.IsDevelopment() && exception is not NotFoundException)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string name, object key) : base($"{name} ({key}) was not found") { }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string error) : this()
    {
        Errors = new Dictionary<string, string[]> { { propertyName, new[] { error } } };
    }
}

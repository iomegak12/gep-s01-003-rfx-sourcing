using EventService.Infrastructure.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Infrastructure.Errors;

/// <summary>
/// Global exception handler that maps unhandled exceptions to RFC 7807
/// <c>application/problem+json</c> responses. Domain-defined exceptions
/// map to specific HTTP status codes; everything else becomes <c>500</c>.
/// </summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;

    public ProblemDetailsExceptionHandler(
        ILogger<ProblemDetailsExceptionHandler> logger,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = Map(exception);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        var correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s
            ? s
            : httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        problem.Extensions["correlationId"] = correlationId;

        if (exception is ValidationException ve && ve.Errors.Count > 0)
        {
            problem.Extensions["errors"] = ve.Errors;
        }

        if (_env.IsDevelopment() && status == StatusCodes.Status500InternalServerError)
        {
            problem.Extensions["exception"] = exception.GetType().FullName;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        _logger.LogError(exception,
            "Unhandled exception mapped to {Status} {Title} for {Path}",
            status, title, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: cancellationToken);

        return true;
    }

    private static (int Status, string Title, string Type) Map(Exception ex) => ex switch
    {
        NotFoundException => (StatusCodes.Status404NotFound,
            "Resource not found",
            "https://datatracker.ietf.org/doc/html/rfc7807#section-3.1"),

        ValidationException => (StatusCodes.Status400BadRequest,
            "Validation failed",
            "https://datatracker.ietf.org/doc/html/rfc7807#section-3.1"),

        ConflictException => (StatusCodes.Status409Conflict,
            "Conflicting state",
            "https://datatracker.ietf.org/doc/html/rfc7807#section-3.1"),

        ForbiddenException => (StatusCodes.Status403Forbidden,
            "Forbidden",
            "https://datatracker.ietf.org/doc/html/rfc7807#section-3.1"),

        _ => (StatusCodes.Status500InternalServerError,
            "Internal server error",
            "https://datatracker.ietf.org/doc/html/rfc7807#section-3.1")
    };
}

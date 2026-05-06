namespace EventService.Infrastructure.Errors;

/// <summary>
/// Base type for application-defined exceptions thrown from the Service layer.
/// The global Problem Details middleware maps each subtype to an HTTP status code.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Resource referenced by the request does not exist. Maps to <c>404 Not Found</c>.</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Request payload failed business or input validation. Maps to <c>400 Bad Request</c>.</summary>
public sealed class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}

/// <summary>Operation conflicts with current resource state (e.g., invalid state transition). Maps to <c>409 Conflict</c>.</summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Caller is authenticated but lacks permission. Maps to <c>403 Forbidden</c>.</summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}

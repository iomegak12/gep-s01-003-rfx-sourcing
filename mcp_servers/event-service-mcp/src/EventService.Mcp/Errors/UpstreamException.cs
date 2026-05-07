namespace EventService.Mcp.Errors;

public sealed class UpstreamException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public string? Detail { get; }
    public string? CorrelationId { get; }

    public UpstreamException(int statusCode, string title, string? detail = null, string? correlationId = null)
        : base($"{statusCode} {title}: {detail}")
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        CorrelationId = correlationId;
    }
}

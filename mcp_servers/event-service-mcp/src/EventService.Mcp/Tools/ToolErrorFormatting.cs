using EventService.Mcp.Errors;

namespace EventService.Mcp.Tools;

public static class ToolErrorFormatting
{
    public static string FormatUpstreamMessage(UpstreamException ex)
    {
        var coreMessage = ex.StatusCode switch
        {
            400 => $"Validation failed: {ex.Detail ?? ex.Title}",
            401 => "Authentication failed. The provided token is missing, invalid, or expired.",
            403 => "Access denied for the current principal.",
            404 => $"Not found: {ex.Detail ?? ex.Title}",
            409 => $"Operation rejected: {ex.Detail ?? ex.Title}",
            _ => $"Upstream error ({ex.StatusCode} {ex.Title}): {ex.Detail}"
        };

        return string.IsNullOrWhiteSpace(ex.CorrelationId)
            ? coreMessage
            : $"{coreMessage} [correlationId={ex.CorrelationId}]";
    }
}

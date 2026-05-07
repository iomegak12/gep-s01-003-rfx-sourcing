namespace EventService.Mcp.Models.Responses;

public sealed record HealthResponse(
    string Status,
    string? Service,
    DateTimeOffset? Timestamp);

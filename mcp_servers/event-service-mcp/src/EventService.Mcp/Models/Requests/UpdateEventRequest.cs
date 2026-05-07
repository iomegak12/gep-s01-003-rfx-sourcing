namespace EventService.Mcp.Models.Requests;

public sealed record UpdateEventRequest(
    string? Title,
    string? Description,
    string? Category,
    DateTimeOffset? ResponseDeadlineUtc);

namespace EventService.Mcp.Models.Requests;

public sealed record CreateEventRequest(
    string Title,
    string? Description,
    string Category,
    string Currency,
    DateTimeOffset ResponseDeadlineUtc);

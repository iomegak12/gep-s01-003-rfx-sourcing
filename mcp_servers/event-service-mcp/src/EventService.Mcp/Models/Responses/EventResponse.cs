namespace EventService.Mcp.Models.Responses;

public sealed record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string Currency,
    DateTimeOffset ResponseDeadlineUtc,
    EventStatus Status,
    int Version,
    string? CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

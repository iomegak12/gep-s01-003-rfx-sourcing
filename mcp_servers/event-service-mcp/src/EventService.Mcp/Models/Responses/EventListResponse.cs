namespace EventService.Mcp.Models.Responses;

public sealed record EventListResponse(
    IReadOnlyList<EventResponse> Items,
    int Limit,
    int Offset,
    int Total);

namespace EventService.Mcp.Models.Responses;

public sealed record LineItemListResponse(IReadOnlyList<LineItemResponse> Items);

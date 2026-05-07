namespace EventService.Mcp.Models.Responses;

public sealed record InvitationListResponse(IReadOnlyList<InvitationResponse> Items);

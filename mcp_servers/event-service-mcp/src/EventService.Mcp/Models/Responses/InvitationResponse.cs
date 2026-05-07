namespace EventService.Mcp.Models.Responses;

public sealed record InvitationResponse(
    Guid EventId,
    Guid SupplierId,
    string SupplierName,
    string? InvitedByUserId,
    DateTimeOffset InvitedAtUtc);

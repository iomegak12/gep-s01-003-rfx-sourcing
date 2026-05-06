namespace EventService.Services.Invitations;

public record InviteSupplierRequest(Guid SupplierId);

public record InvitationResponse(
    Guid EventId,
    Guid SupplierId,
    string SupplierName,
    string InvitedByUserId,
    DateTime InvitedAtUtc);

public record InvitationListResponse(IReadOnlyList<InvitationResponse> Items);

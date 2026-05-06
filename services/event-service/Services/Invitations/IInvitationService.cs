namespace EventService.Services.Invitations;

/// <summary>Business operations for inviting suppliers to a Draft sourcing event.</summary>
public interface IInvitationService
{
    Task<InvitationResponse> InviteAsync(Guid eventId, Guid supplierId, string userId, string correlationId, CancellationToken ct = default);
    Task<InvitationListResponse> ListByEventAsync(Guid eventId, CancellationToken ct = default);
    Task RevokeAsync(Guid eventId, Guid supplierId, string userId, string correlationId, CancellationToken ct = default);
}

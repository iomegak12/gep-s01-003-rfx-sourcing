using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Infrastructure.Errors;
using EventService.Repositories;
using EventService.Services.Audit;

namespace EventService.Services.Invitations;

/// <summary>Enforces invitation business rules and delegates persistence to repositories.</summary>
public sealed class InvitationService : IInvitationService
{
    private readonly IInvitationRepository _invitations;
    private readonly IEventRepository _events;
    private readonly ISupplierRepository _suppliers;
    private readonly IAuditService _audit;

    public InvitationService(
        IInvitationRepository invitations,
        IEventRepository events,
        ISupplierRepository suppliers,
        IAuditService audit)
    {
        _invitations = invitations;
        _events = events;
        _suppliers = suppliers;
        _audit = audit;
    }

    public async Task<InvitationResponse> InviteAsync(
        Guid eventId, Guid supplierId, string userId, string correlationId, CancellationToken ct = default)
    {
        var e = await _events.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException($"Event '{eventId}' not found.");

        if (e.Status != EventStatus.Draft)
            throw new ConflictException($"Event '{eventId}' is in '{e.Status}' status — invitations can only be sent for Draft events.");

        var supplier = await _suppliers.GetByIdAsync(supplierId, ct)
            ?? throw new NotFoundException($"Supplier '{supplierId}' not found.");

        if (!supplier.IsActive)
            throw new ConflictException($"Supplier '{supplierId}' is inactive and cannot be invited.");

        var existing = await _invitations.GetAsync(eventId, supplierId, ct);
        if (existing is not null)
            throw new ConflictException($"Supplier '{supplierId}' is already invited to event '{eventId}'.");

        var invitation = new EventSupplier
        {
            EventId = eventId,
            SupplierId = supplierId,
            InvitedByUserId = userId,
            InvitedAtUtc = DateTime.UtcNow
        };

        await _invitations.AddAsync(invitation, ct);
        await _audit.RecordAsync(eventId, AuditAction.InvitationSent, userId, correlationId, ct);

        return new InvitationResponse(eventId, supplierId, supplier.Name, userId, invitation.InvitedAtUtc);
    }

    public async Task<InvitationListResponse> ListByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var exists = await _events.GetByIdAsync(eventId, ct);
        if (exists is null)
            throw new NotFoundException($"Event '{eventId}' not found.");

        var items = await _invitations.ListByEventAsync(eventId, ct);

        return new InvitationListResponse(
            items.Select(es => new InvitationResponse(
                es.EventId, es.SupplierId, es.Supplier.Name, es.InvitedByUserId, es.InvitedAtUtc))
            .ToList());
    }

    public async Task RevokeAsync(
        Guid eventId, Guid supplierId, string userId, string correlationId, CancellationToken ct = default)
    {
        var e = await _events.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException($"Event '{eventId}' not found.");

        if (e.Status != EventStatus.Draft)
            throw new ConflictException($"Event '{eventId}' is in '{e.Status}' status — invitations can only be revoked from Draft events.");

        var invitation = await _invitations.GetAsync(eventId, supplierId, ct)
            ?? throw new NotFoundException($"Supplier '{supplierId}' is not invited to event '{eventId}'.");

        await _invitations.DeleteAsync(invitation, ct);
        await _audit.RecordAsync(eventId, AuditAction.InvitationRevoked, userId, correlationId, ct);
    }
}

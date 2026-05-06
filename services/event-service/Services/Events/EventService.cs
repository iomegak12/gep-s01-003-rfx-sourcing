using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Infrastructure.Errors;
using EventService.Repositories;
using EventService.Services.Audit;

namespace EventService.Services.Events;

/// <summary>Implements business rules for the sourcing event lifecycle.</summary>
public sealed class EventService : IEventService
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;

    private readonly IEventRepository _events;
    private readonly ILineItemRepository _lineItems;
    private readonly IInvitationRepository _invitations;
    private readonly IAuditService _audit;

    public EventService(
        IEventRepository events,
        ILineItemRepository lineItems,
        IInvitationRepository invitations,
        IAuditService audit)
    {
        _events = events;
        _lineItems = lineItems;
        _invitations = invitations;
        _audit = audit;
    }

    public async Task<EventResponse> CreateAsync(
        CreateEventRequest request, string userId, string correlationId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var e = new Event
        {
            Id = Guid.CreateVersion7(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category.Trim(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            ResponseDeadlineUtc = request.ResponseDeadlineUtc.ToUniversalTime(),
            Status = EventStatus.Draft,
            Version = 0,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _events.AddAsync(e, ct);
        await _audit.RecordAsync(e.Id, AuditAction.EventCreated, userId, correlationId, ct);

        return ToResponse(e);
    }

    public async Task<EventListResponse> ListAsync(int limit, int offset, CancellationToken ct = default)
    {
        var clampedLimit = Math.Clamp(limit <= 0 ? DefaultLimit : limit, 1, MaxLimit);
        var clampedOffset = Math.Max(0, offset);

        var (items, total) = await _events.ListAsync(clampedLimit, clampedOffset, ct);

        return new EventListResponse(
            items.Select(ToResponse).ToList(),
            clampedLimit,
            clampedOffset,
            total);
    }

    public async Task<EventResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _events.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Event '{id}' not found.");

        return ToResponse(e);
    }

    public async Task<EventResponse> UpdateAsync(
        Guid id, UpdateEventRequest request, string userId, string correlationId, CancellationToken ct = default)
    {
        var e = await _events.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Event '{id}' not found.");

        if (e.Status != EventStatus.Draft)
            throw new ConflictException($"Event '{id}' is in '{e.Status}' status and cannot be modified.");

        if (request.Title is not null)        e.Title = request.Title.Trim();
        if (request.Description is not null)  e.Description = request.Description.Trim();
        if (request.Category is not null)     e.Category = request.Category.Trim();
        if (request.ResponseDeadlineUtc.HasValue)
            e.ResponseDeadlineUtc = request.ResponseDeadlineUtc.Value.ToUniversalTime();

        e.Version++;
        e.UpdatedAtUtc = DateTime.UtcNow;

        await _events.UpdateAsync(e, ct);
        await _audit.RecordAsync(e.Id, AuditAction.EventUpdated, userId, correlationId, ct);

        return ToResponse(e);
    }

    public async Task<EventResponse> PublishAsync(
        Guid id, string userId, string correlationId, CancellationToken ct = default)
    {
        var e = await _events.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Event '{id}' not found.");

        if (e.Status != EventStatus.Draft)
            throw new ConflictException($"Event '{id}' is in '{e.Status}' status and cannot be published.");

        if (e.ResponseDeadlineUtc <= DateTime.UtcNow)
            throw new ConflictException($"Event '{id}' cannot be published — response deadline has already passed.");

        var lineItemCount = await _lineItems.CountByEventAsync(id, ct);
        if (lineItemCount == 0)
            throw new ConflictException($"Event '{id}' cannot be published — at least one line item is required.");

        var invitationCount = await _invitations.CountByEventAsync(id, ct);
        if (invitationCount == 0)
            throw new ConflictException($"Event '{id}' cannot be published — at least one supplier must be invited.");

        // TODO(BSS-INTEGRATION): enforce criteria-count gate once BSS exposes
        // GET /api/v1/events/{id}/criteria/count. Until then this gate is skipped.
        // See docs/impl-event-service-plan.md Phase 6.

        e.Status = EventStatus.Published;
        e.Version++;
        e.UpdatedAtUtc = DateTime.UtcNow;

        await _events.UpdateAsync(e, ct);
        await _audit.RecordAsync(e.Id, AuditAction.EventPublished, userId, correlationId, ct);

        return ToResponse(e);
    }

    private static EventResponse ToResponse(Event e) => new(
        e.Id, e.Title, e.Description, e.Category, e.Currency,
        e.ResponseDeadlineUtc, e.Status, e.Version,
        e.CreatedByUserId, e.CreatedAtUtc, e.UpdatedAtUtc);
}

using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Infrastructure.Errors;
using EventService.Repositories;
using EventService.Services.Audit;

namespace EventService.Services.LineItems;

/// <summary>Manages line items belonging to a Draft sourcing event.</summary>
public sealed class LineItemService : ILineItemService
{
    private readonly ILineItemRepository _lineItems;
    private readonly IEventRepository _events;
    private readonly IAuditService _audit;

    public LineItemService(ILineItemRepository lineItems, IEventRepository events, IAuditService audit)
    {
        _lineItems = lineItems;
        _events = events;
        _audit = audit;
    }

    public async Task<LineItemResponse> AddAsync(
        Guid eventId, AddLineItemRequest request, string userId, string correlationId, CancellationToken ct = default)
    {
        var e = await _events.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException($"Event '{eventId}' not found.");

        if (e.Status != EventStatus.Draft)
            throw new ConflictException($"Event '{eventId}' is in '{e.Status}' status — line items can only be added to Draft events.");

        var item = new LineItem
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Currency = e.Currency,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _lineItems.AddAsync(item, ct);
        await _audit.RecordAsync(eventId, AuditAction.LineItemAdded, userId, correlationId, ct);

        return ToResponse(item);
    }

    public async Task<LineItemListResponse> ListByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var exists = await _events.GetByIdAsync(eventId, ct);
        if (exists is null)
            throw new NotFoundException($"Event '{eventId}' not found.");

        var items = await _lineItems.ListByEventAsync(eventId, ct);
        return new LineItemListResponse(items.Select(ToResponse).ToList());
    }

    public async Task DeleteAsync(
        Guid eventId, Guid itemId, string userId, string correlationId, CancellationToken ct = default)
    {
        var e = await _events.GetByIdAsync(eventId, ct)
            ?? throw new NotFoundException($"Event '{eventId}' not found.");

        if (e.Status != EventStatus.Draft)
            throw new ConflictException($"Event '{eventId}' is in '{e.Status}' status — line items can only be removed from Draft events.");

        var item = await _lineItems.GetByIdAsync(itemId, ct);
        if (item is null || item.EventId != eventId)
            throw new NotFoundException($"Line item '{itemId}' not found on event '{eventId}'.");

        await _lineItems.DeleteAsync(item, ct);
        await _audit.RecordAsync(eventId, AuditAction.LineItemRemoved, userId, correlationId, ct);
    }

    private static LineItemResponse ToResponse(LineItem l) => new(
        l.Id, l.EventId, l.Description, l.Quantity, l.UnitPrice, l.Currency, l.CreatedAtUtc);
}

namespace EventService.Services.LineItems;

/// <summary>Business operations for line items scoped to a sourcing event.</summary>
public interface ILineItemService
{
    Task<LineItemResponse> AddAsync(Guid eventId, AddLineItemRequest request, string userId, string correlationId, CancellationToken ct = default);
    Task<LineItemListResponse> ListByEventAsync(Guid eventId, CancellationToken ct = default);
    Task DeleteAsync(Guid eventId, Guid itemId, string userId, string correlationId, CancellationToken ct = default);
}

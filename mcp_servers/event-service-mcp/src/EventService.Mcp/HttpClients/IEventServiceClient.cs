using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;

namespace EventService.Mcp.HttpClients;

public interface IEventServiceClient
{
    // Suppliers
    Task<SupplierListResponse> ListSuppliersAsync(int limit, int offset, bool activeOnly, CancellationToken ct);
    Task<SupplierResponse> GetSupplierAsync(Guid id, CancellationToken ct);

    // Events
    Task<EventResponse> CreateEventAsync(CreateEventRequest req, CancellationToken ct);
    Task<EventListResponse> ListEventsAsync(int limit, int offset, CancellationToken ct);
    Task<EventResponse> GetEventAsync(Guid id, CancellationToken ct);
    Task<EventResponse> UpdateEventAsync(Guid id, UpdateEventRequest req, CancellationToken ct);
    Task<EventResponse> PublishEventAsync(Guid id, CancellationToken ct);

    // Line items
    Task<LineItemResponse> AddLineItemAsync(Guid eventId, AddLineItemRequest req, CancellationToken ct);
    Task<LineItemListResponse> ListLineItemsAsync(Guid eventId, CancellationToken ct);
    Task DeleteLineItemAsync(Guid eventId, Guid itemId, CancellationToken ct);

    // Invitations
    Task<InvitationResponse> InviteSupplierAsync(Guid eventId, InviteSupplierRequest req, CancellationToken ct);
    Task<InvitationListResponse> ListInvitationsAsync(Guid eventId, CancellationToken ct);
    Task RevokeInvitationAsync(Guid eventId, Guid supplierId, CancellationToken ct);
}

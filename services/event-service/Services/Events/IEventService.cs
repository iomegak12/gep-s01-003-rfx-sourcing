namespace EventService.Services.Events;

/// <summary>Business operations for the sourcing event aggregate.</summary>
public interface IEventService
{
    Task<EventResponse> CreateAsync(CreateEventRequest request, string userId, string correlationId, CancellationToken ct = default);
    Task<EventListResponse> ListAsync(int limit, int offset, CancellationToken ct = default);
    Task<EventResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EventResponse> UpdateAsync(Guid id, UpdateEventRequest request, string userId, string correlationId, CancellationToken ct = default);
    Task<EventResponse> PublishAsync(Guid id, string userId, string correlationId, CancellationToken ct = default);
}

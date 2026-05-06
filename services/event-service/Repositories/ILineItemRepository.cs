using EventService.Domain.Entities;

namespace EventService.Repositories;

/// <summary>Persistence contract for <see cref="LineItem"/> entities scoped to an event.</summary>
public interface ILineItemRepository
{
    Task<LineItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LineItem>> ListByEventAsync(Guid eventId, CancellationToken ct = default);
    Task<int> CountByEventAsync(Guid eventId, CancellationToken ct = default);
    Task AddAsync(LineItem item, CancellationToken ct = default);
    Task DeleteAsync(LineItem item, CancellationToken ct = default);
}

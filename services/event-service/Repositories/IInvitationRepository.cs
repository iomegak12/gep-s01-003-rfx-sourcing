using EventService.Domain.Entities;

namespace EventService.Repositories;

/// <summary>Persistence contract for event-supplier invitation join records.</summary>
public interface IInvitationRepository
{
    Task<EventSupplier?> GetAsync(Guid eventId, Guid supplierId, CancellationToken ct = default);
    Task<IReadOnlyList<EventSupplier>> ListByEventAsync(Guid eventId, CancellationToken ct = default);
    Task<int> CountByEventAsync(Guid eventId, CancellationToken ct = default);
    Task AddAsync(EventSupplier invitation, CancellationToken ct = default);
    Task DeleteAsync(EventSupplier invitation, CancellationToken ct = default);
}

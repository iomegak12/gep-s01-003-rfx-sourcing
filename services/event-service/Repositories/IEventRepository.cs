using EventService.Domain.Entities;

namespace EventService.Repositories;

/// <summary>Persistence contract for the <see cref="Event"/> aggregate root.</summary>
public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Event> Items, int Total)> ListAsync(int limit, int offset, CancellationToken ct = default);
    Task AddAsync(Event e, CancellationToken ct = default);
    Task UpdateAsync(Event e, CancellationToken ct = default);
}

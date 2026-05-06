using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Repositories;

/// <summary>EF Core implementation of <see cref="ILineItemRepository"/>.</summary>
public sealed class LineItemRepository : ILineItemRepository
{
    private readonly EventDbContext _db;

    public LineItemRepository(EventDbContext db) => _db = db;

    public Task<LineItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.LineItems.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<LineItem>> ListByEventAsync(Guid eventId, CancellationToken ct = default)
        => await _db.LineItems.AsNoTracking()
            .Where(l => l.EventId == eventId)
            .OrderBy(l => l.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<int> CountByEventAsync(Guid eventId, CancellationToken ct = default)
        => _db.LineItems.CountAsync(l => l.EventId == eventId, ct);

    public async Task AddAsync(LineItem item, CancellationToken ct = default)
    {
        _db.LineItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(LineItem item, CancellationToken ct = default)
    {
        _db.LineItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }
}

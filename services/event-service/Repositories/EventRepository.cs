using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Repositories;

/// <summary>EF Core implementation of <see cref="IEventRepository"/>.</summary>
public sealed class EventRepository : IEventRepository
{
    private readonly EventDbContext _db;

    public EventRepository(EventDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<(IReadOnlyList<Event> Items, int Total)> ListAsync(
        int limit, int offset, CancellationToken ct = default)
    {
        var query = _db.Events.AsNoTracking().OrderBy(e => e.CreatedAtUtc);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(offset).Take(limit).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Event e, CancellationToken ct = default)
    {
        _db.Events.Add(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Event e, CancellationToken ct = default)
    {
        _db.Events.Update(e);
        await _db.SaveChangesAsync(ct);
    }
}

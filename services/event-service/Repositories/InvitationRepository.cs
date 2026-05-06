using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Repositories;

/// <summary>EF Core implementation of <see cref="IInvitationRepository"/>.</summary>
public sealed class InvitationRepository : IInvitationRepository
{
    private readonly EventDbContext _db;

    public InvitationRepository(EventDbContext db) => _db = db;

    public Task<EventSupplier?> GetAsync(Guid eventId, Guid supplierId, CancellationToken ct = default)
        => _db.EventSuppliers
              .AsNoTracking()
              .Include(es => es.Supplier)
              .FirstOrDefaultAsync(es => es.EventId == eventId && es.SupplierId == supplierId, ct);

    public async Task<IReadOnlyList<EventSupplier>> ListByEventAsync(Guid eventId, CancellationToken ct = default)
        => await _db.EventSuppliers
                    .AsNoTracking()
                    .Include(es => es.Supplier)
                    .Where(es => es.EventId == eventId)
                    .OrderBy(es => es.Supplier.Name)
                    .ToListAsync(ct);

    public Task<int> CountByEventAsync(Guid eventId, CancellationToken ct = default)
        => _db.EventSuppliers.CountAsync(es => es.EventId == eventId, ct);

    public async Task AddAsync(EventSupplier invitation, CancellationToken ct = default)
    {
        _db.EventSuppliers.Add(invitation);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EventSupplier invitation, CancellationToken ct = default)
    {
        _db.EventSuppliers.Remove(invitation);
        await _db.SaveChangesAsync(ct);
    }
}

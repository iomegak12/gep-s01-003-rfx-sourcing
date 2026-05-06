using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Repositories;

/// <summary>EF Core implementation of <see cref="ISupplierRepository"/>.</summary>
public sealed class SupplierRepository : ISupplierRepository
{
    private readonly EventDbContext _db;

    public SupplierRepository(EventDbContext db)
    {
        _db = db;
    }

    public Task<int> CountAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var q = _db.Suppliers.AsNoTracking();
        if (activeOnly) q = q.Where(s => s.IsActive);
        return q.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Supplier>> ListAsync(
        bool activeOnly,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var q = _db.Suppliers.AsNoTracking();
        if (activeOnly) q = q.Where(s => s.IsActive);

        return await q
            .OrderBy(s => s.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
        => _db.Suppliers.AsNoTracking().AnyAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Supplier> suppliers, CancellationToken cancellationToken)
    {
        await _db.Suppliers.AddRangeAsync(suppliers, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

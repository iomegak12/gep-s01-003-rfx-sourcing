using EventService.Domain.Entities;

namespace EventService.Repositories;

/// <summary>EF Core implementation of <see cref="IAuditRepository"/>.</summary>
public sealed class AuditRepository : IAuditRepository
{
    private readonly EventDbContext _db;

    public AuditRepository(EventDbContext db) => _db = db;

    public async Task AddAsync(AuditEvent audit, CancellationToken ct = default)
    {
        _db.AuditEvents.Add(audit);
        await _db.SaveChangesAsync(ct);
    }
}

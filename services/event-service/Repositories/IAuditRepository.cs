using EventService.Domain.Entities;

namespace EventService.Repositories;

/// <summary>Persistence contract for append-only <see cref="AuditEvent"/> records.</summary>
public interface IAuditRepository
{
    Task AddAsync(AuditEvent audit, CancellationToken ct = default);
}

using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Repositories;

namespace EventService.Services.Audit;

/// <summary>Writes append-only audit entries via <see cref="IAuditRepository"/>.</summary>
public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _repo;

    public AuditService(IAuditRepository repo) => _repo = repo;

    public Task RecordAsync(Guid eventId, AuditAction action, string userId, string correlationId, CancellationToken ct = default)
        => _repo.AddAsync(new AuditEvent
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            Action = action,
            CorrelationId = correlationId,
            PerformedByUserId = userId,
            OccurredAtUtc = DateTime.UtcNow
        }, ct);
}

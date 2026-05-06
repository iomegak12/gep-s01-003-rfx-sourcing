using EventService.Domain.Enums;

namespace EventService.Services.Audit;

/// <summary>Appends structured audit entries for sourcing-event domain actions.</summary>
public interface IAuditService
{
    Task RecordAsync(Guid eventId, AuditAction action, string userId, string correlationId, CancellationToken ct = default);
}

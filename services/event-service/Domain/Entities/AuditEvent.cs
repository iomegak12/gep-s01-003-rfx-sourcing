using EventService.Domain.Enums;

namespace EventService.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public AuditAction Action { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Payload { get; set; }
}

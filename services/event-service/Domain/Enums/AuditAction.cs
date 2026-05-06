namespace EventService.Domain.Enums;

public enum AuditAction
{
    EventCreated = 0,
    EventUpdated = 1,
    EventPublished = 2,
    EventAwarded = 3,    // TODO(BSS-INTEGRATION): driven by BSS award flow
    LineItemAdded = 4,
    LineItemRemoved = 5,
    InvitationSent = 6,
    InvitationRevoked = 7
}

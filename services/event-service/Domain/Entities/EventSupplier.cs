namespace EventService.Domain.Entities;

public class EventSupplier
{
    public Guid EventId { get; set; }
    public Guid SupplierId { get; set; }
    public string InvitedByUserId { get; set; } = string.Empty;
    public DateTime InvitedAtUtc { get; set; }

    public Supplier Supplier { get; set; } = null!;
}

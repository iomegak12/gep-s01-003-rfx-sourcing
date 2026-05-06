namespace EventService.Domain.Entities;

public class LineItem
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime CreatedAtUtc { get; set; }

    public Event Event { get; set; } = null!;
}

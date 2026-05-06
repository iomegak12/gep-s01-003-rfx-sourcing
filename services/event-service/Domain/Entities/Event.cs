using EventService.Domain.Enums;

namespace EventService.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public DateTime ResponseDeadlineUtc { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public int Version { get; set; } = 0;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

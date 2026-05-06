namespace EventService.Domain.Enums;

public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Bidding = 2,     // advisory — driven by BSS activity
    Scored = 3,      // advisory — driven by BSS activity
    Awarded = 4,
    Closed = 5
}

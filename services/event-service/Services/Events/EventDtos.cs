using EventService.Domain.Enums;

namespace EventService.Services.Events;

public record CreateEventRequest(
    string Title,
    string? Description,
    string Category,
    string Currency,
    DateTime ResponseDeadlineUtc);

public record UpdateEventRequest(
    string? Title,
    string? Description,
    string? Category,
    DateTime? ResponseDeadlineUtc);

public record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string Currency,
    DateTime ResponseDeadlineUtc,
    EventStatus Status,
    int Version,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record EventListResponse(
    IReadOnlyList<EventResponse> Items,
    int Limit,
    int Offset,
    int Total);

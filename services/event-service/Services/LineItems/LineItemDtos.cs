namespace EventService.Services.LineItems;

public record AddLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record LineItemResponse(
    Guid Id,
    Guid EventId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string Currency,
    DateTime CreatedAtUtc);

public record LineItemListResponse(IReadOnlyList<LineItemResponse> Items);

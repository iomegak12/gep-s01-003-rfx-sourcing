namespace EventService.Mcp.Models.Responses;

public sealed record LineItemResponse(
    Guid Id,
    Guid EventId,
    string Description,
    double Quantity,
    double UnitPrice,
    string Currency,
    DateTimeOffset CreatedAtUtc);

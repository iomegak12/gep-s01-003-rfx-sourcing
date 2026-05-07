namespace EventService.Mcp.Models.Requests;

public sealed record AddLineItemRequest(
    string Description,
    double Quantity,
    double UnitPrice);

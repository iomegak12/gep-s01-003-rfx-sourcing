namespace EventService.Mcp.Models.Responses;

public sealed record SupplierListResponse(
    IReadOnlyList<SupplierResponse> Items,
    int Limit,
    int Offset,
    int Total);

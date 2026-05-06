namespace EventService.Services.Suppliers;

public sealed record SupplierResponse(
    Guid Id,
    string Name,
    string ContactEmail,
    bool IsActive);

public sealed record SupplierListResponse(
    IReadOnlyList<SupplierResponse> Items,
    int Limit,
    int Offset,
    int Total);

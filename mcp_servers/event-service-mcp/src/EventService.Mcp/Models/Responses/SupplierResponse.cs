namespace EventService.Mcp.Models.Responses;

public sealed record SupplierResponse(
    Guid Id,
    string Name,
    string? ContactEmail,
    bool IsActive);

namespace EventService.Services.Suppliers;

/// <summary>
/// Application-layer operations for the read-mostly supplier master list.
/// </summary>
public interface ISupplierService
{
    /// <summary>Returns a paginated list of suppliers.</summary>
    /// <param name="activeOnly">If true, only suppliers with <c>IsActive == true</c> are returned. Default <c>true</c>.</param>
    /// <param name="limit">Page size; clamped to <c>[1, 100]</c>. Default <c>50</c>.</param>
    /// <param name="offset">Number of suppliers to skip; clamped to <c>&gt;= 0</c>. Default <c>0</c>.</param>
    Task<SupplierListResponse> ListAsync(
        bool activeOnly,
        int limit,
        int offset,
        CancellationToken cancellationToken);

    /// <summary>Returns a supplier by id. Throws <c>NotFoundException</c> if absent.</summary>
    Task<SupplierResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

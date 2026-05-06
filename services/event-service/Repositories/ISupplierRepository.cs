using EventService.Domain.Entities;

namespace EventService.Repositories;

/// <summary>
/// Persistence-layer abstraction for the read-mostly supplier master list.
/// Implementations return Domain entities only.
/// </summary>
public interface ISupplierRepository
{
    /// <summary>Returns the total number of suppliers, optionally filtered by active flag.</summary>
    Task<int> CountAsync(bool activeOnly, CancellationToken cancellationToken);

    /// <summary>Returns a paginated page of suppliers ordered by name.</summary>
    Task<IReadOnlyList<Supplier>> ListAsync(
        bool activeOnly,
        int limit,
        int offset,
        CancellationToken cancellationToken);

    /// <summary>Returns a supplier by id, or <c>null</c> if not found.</summary>
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns true if the database already contains at least one supplier (used by seed gate).</summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken);

    /// <summary>Adds the given suppliers in a single transaction (used by the seed runner).</summary>
    Task AddRangeAsync(IEnumerable<Supplier> suppliers, CancellationToken cancellationToken);
}

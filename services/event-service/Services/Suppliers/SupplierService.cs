using EventService.Domain.Entities;
using EventService.Infrastructure.Errors;
using EventService.Repositories;

namespace EventService.Services.Suppliers;

/// <summary>Default <see cref="ISupplierService"/> implementation backed by <see cref="ISupplierRepository"/>.</summary>
public sealed class SupplierService : ISupplierService
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    private readonly ISupplierRepository _repo;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(ISupplierRepository repo, ILogger<SupplierService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<SupplierListResponse> ListAsync(
        bool activeOnly,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit <= 0 ? DefaultLimit : limit, 1, MaxLimit);
        offset = Math.Max(0, offset);

        var total = await _repo.CountAsync(activeOnly, cancellationToken);
        var page = await _repo.ListAsync(activeOnly, limit, offset, cancellationToken);

        _logger.LogDebug("Listed suppliers activeOnly={ActiveOnly} limit={Limit} offset={Offset} total={Total}",
            activeOnly, limit, offset, total);

        var items = page.Select(ToResponse).ToList();
        return new SupplierListResponse(items, limit, offset, total);
    }

    public async Task<SupplierResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Supplier '{id}' not found.");

        return ToResponse(supplier);
    }

    private static SupplierResponse ToResponse(Supplier s) =>
        new(s.Id, s.Name, s.ContactEmail, s.IsActive);
}

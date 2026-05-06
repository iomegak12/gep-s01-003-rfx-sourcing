using EventService.Repositories;

namespace EventService.Infrastructure.Persistence;

/// <summary>
/// Runs idempotent seed-data load on startup when
/// <c>Persistence:SeedDataEnabled</c> is true. A non-empty target table is
/// treated as "already seeded" and the load is skipped.
/// </summary>
public sealed class SeedRunner
{
    private readonly IServiceProvider _services;
    private readonly PersistenceOptions _options;
    private readonly ILogger<SeedRunner> _logger;

    public SeedRunner(
        IServiceProvider services,
        PersistenceOptions options,
        ILogger<SeedRunner> logger)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!_options.SeedDataEnabled)
        {
            _logger.LogInformation("Seed data disabled; skipping.");
            return;
        }

        await using var scope = _services.CreateAsyncScope();
        var supplierRepo = scope.ServiceProvider.GetRequiredService<ISupplierRepository>();

        if (await supplierRepo.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Suppliers table is non-empty; skipping seed.");
            return;
        }

        await supplierRepo.AddRangeAsync(SupplierSeed.Records, cancellationToken);
        _logger.LogInformation("Seeded {Count} suppliers.", SupplierSeed.Records.Count);
    }
}

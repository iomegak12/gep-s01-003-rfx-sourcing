using EventService.Repositories;
using EventService.Services.Audit;
using EventService.Services.Events;
using EventService.Services.Invitations;
using EventService.Services.LineItems;
using EventService.Services.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence;

/// <summary>
/// DI extensions that register the EF Core DbContext against the configured
/// SQLite database path and ensure the parent directory exists.
/// </summary>
public static class PersistenceExtensions
{
    public static IServiceCollection AddEventServicePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>()
            ?? new PersistenceOptions();

        services.AddSingleton(options);

        var dbPath = Path.GetFullPath(options.DatabasePath);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        services.AddDbContext<EventDbContext>(builder =>
            builder.UseSqlite($"Data Source={dbPath}"));

        // Repositories
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ILineItemRepository, LineItemRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();

        // Application services
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEventService, EventService.Services.Events.EventService>();
        services.AddScoped<ILineItemService, LineItemService>();
        services.AddScoped<IInvitationService, InvitationService>();

        // Seed runner
        services.AddSingleton<SeedRunner>();

        return services;
    }

    /// <summary>Runs idempotent seed-data load if enabled in configuration.</summary>
    public static async Task RunSeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var runner = services.GetRequiredService<SeedRunner>();
        await runner.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Applies pending EF Core migrations on startup if enabled in configuration.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        var options = services.GetRequiredService<PersistenceOptions>();
        if (!options.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        await db.Database.MigrateAsync();
    }
}

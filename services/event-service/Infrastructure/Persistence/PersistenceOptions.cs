namespace EventService.Infrastructure.Persistence;

/// <summary>
/// Strongly-typed binding for the <c>Persistence</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    /// <summary>Relative or absolute path to the SQLite database file.</summary>
    public string DatabasePath { get; init; } = "./data/event_service.db";

    /// <summary>If true, EF Core migrations are applied on startup.</summary>
    public bool ApplyMigrationsOnStartup { get; init; } = true;

    /// <summary>If true, idempotent seed data is loaded on startup.</summary>
    public bool SeedDataEnabled { get; init; } = true;
}

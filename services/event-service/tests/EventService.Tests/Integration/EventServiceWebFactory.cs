using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EventService.Tests.Integration;

/// <summary>
/// Spins up the full ASP.NET Core pipeline against a per-instance SQLite file
/// so each test class gets an isolated, clean database.
/// </summary>
internal sealed class EventServiceWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"evt_test_{Guid.NewGuid():N}.db");

    public string DbPath => _dbPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:DatabasePath"]          = _dbPath,
                ["Persistence:ApplyMigrationsOnStartup"] = "true",
                ["Persistence:SeedDataEnabled"]       = "true",
                ["Jwt:SigningKey"]  = "bsprTMgGLN862rt3oy1Qeh289u0lWn6yOSBijQrDWDTxd44kiV2MsPgRrVd",
                ["Jwt:Issuer"]     = "rfx-auth-service",
                ["Jwt:Audience"]   = "rfx-sourcing-system"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

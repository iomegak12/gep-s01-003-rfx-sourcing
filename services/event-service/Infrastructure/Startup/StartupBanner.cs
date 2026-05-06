using EventService.Infrastructure.Auth;
using EventService.Infrastructure.Persistence;
using EventService.Repositories;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace EventService.Infrastructure.Startup;

/// <summary>
/// Renders a colourful banner to the console summarising service runtime
/// information: environment, version, bind URL, database path and readiness,
/// API surface URLs (OpenAPI, Swagger, Health), and JWT issuer/audience.
/// </summary>
public static class StartupBanner
{
    public static async Task RenderAsync(WebApplication app)
    {
        var env = app.Environment;
        var persistence = app.Services.GetRequiredService<PersistenceOptions>();
        var jwt = app.Services.GetRequiredService<JwtOptions>();
        var version = typeof(StartupBanner).Assembly.GetName().Version?.ToString() ?? "unknown";

        var bindUrl = app.Urls.FirstOrDefault()
            ?? app.Configuration["Kestrel:Endpoints:Http:Url"]
            ?? "http://0.0.0.0:5001";

        var dbStatus = await PingDatabaseAsync(app.Services);

        AnsiConsole.Write(
            new FigletText("Event Service")
                .Centered()
                .Color(Color.DeepSkyBlue1));

        AnsiConsole.Write(new Rule("[bold deepskyblue1]RFx Sourcing — Event Service[/]")
        {
            Justification = Justify.Center
        });

        var info = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.DeepSkyBlue1)
            .AddColumn(new TableColumn("[bold]Setting[/]").Width(28))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        info.AddRow("[grey]Environment[/]", $"[yellow]{env.EnvironmentName}[/]");
        info.AddRow("[grey]Assembly version[/]", $"[white]{version}[/]");
        info.AddRow("[grey]Bind URL[/]", $"[green]{bindUrl}[/]");
        info.AddRow("[grey]Database path[/]", $"[white]{persistence.DatabasePath}[/]");
        info.AddRow("[grey]Database readiness[/]", dbStatus.Ok
            ? "[bold green]✓ Ready[/]"
            : $"[bold red]✗ {dbStatus.Message}[/]");
        info.AddRow("[grey]Migrations on startup[/]",
            persistence.ApplyMigrationsOnStartup ? "[green]enabled[/]" : "[yellow]disabled[/]");
        info.AddRow("[grey]Seed data[/]",
            persistence.SeedDataEnabled ? "[green]enabled[/]" : "[yellow]disabled[/]");
        info.AddRow("[grey]JWT issuer[/]", $"[white]{jwt.Issuer}[/]");
        info.AddRow("[grey]JWT audience[/]", $"[white]{jwt.Audience}[/]");

        AnsiConsole.Write(info);

        var endpoints = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.MediumPurple)
            .Title("[bold mediumpurple]API endpoints[/]")
            .AddColumn(new TableColumn("[bold]Endpoint[/]").Width(28))
            .AddColumn(new TableColumn("[bold]URL[/]"));

        endpoints.AddRow("Health (liveness)",          $"[green]{bindUrl}/api/v1/health[/]");
        endpoints.AddRow("OpenAPI document",           $"[cyan]{bindUrl}/api/v1/openapi.json[/]");
        endpoints.AddRow("Swagger UI",                 $"[cyan]{bindUrl}/swagger[/]");
        endpoints.AddRow("Suppliers — list",           $"[white]{bindUrl}/api/v1/suppliers[/]");
        endpoints.AddRow("Suppliers — by id",          $"[white]{bindUrl}/api/v1/suppliers/{{id}}[/]");
        endpoints.AddRow("Events — create / list",     $"[white]{bindUrl}/api/v1/events[/]");
        endpoints.AddRow("Events — get / update",      $"[white]{bindUrl}/api/v1/events/{{id}}[/]");
        endpoints.AddRow("Line items — add / list",    $"[white]{bindUrl}/api/v1/events/{{id}}/line-items[/]");
        endpoints.AddRow("Line items — delete",        $"[white]{bindUrl}/api/v1/events/{{id}}/line-items/{{itemId}}[/]");
        endpoints.AddRow("Invitations — send / list",  $"[white]{bindUrl}/api/v1/events/{{id}}/invitations[/]");
        endpoints.AddRow("Invitations — revoke",       $"[white]{bindUrl}/api/v1/events/{{id}}/invitations/{{supplierId}}[/]");
        endpoints.AddRow("Events — publish",           $"[white]{bindUrl}/api/v1/events/{{id}}/publish[/]");

        AnsiConsole.Write(endpoints);

        AnsiConsole.MarkupLine("[grey]Press[/] [bold yellow]Ctrl+C[/] [grey]to shut down gracefully.[/]");
        AnsiConsole.WriteLine();
    }

    private static async Task<(bool Ok, string Message)> PingDatabaseAsync(IServiceProvider services)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            var canConnect = await db.Database.CanConnectAsync();
            return canConnect
                ? (true, "Ready")
                : (false, "CanConnect returned false");
        }
        catch (Exception ex)
        {
            return (false, ex.GetType().Name + ": " + ex.Message);
        }
    }
}

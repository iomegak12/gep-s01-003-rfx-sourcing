using EventService.Infrastructure.Auth;
using EventService.Infrastructure.Errors;
using EventService.Infrastructure.Logging;
using EventService.Infrastructure.Persistence;
using EventService.Infrastructure.Startup;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext());

// ── MVC + validation ─────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Auth (JWT + Buyer policy) ────────────────────────────────────────────────
builder.Services.AddEventServiceAuth(builder.Configuration);

// ── Persistence (EF Core + SQLite) ───────────────────────────────────────────
builder.Services.AddEventServicePersistence(builder.Configuration);

// ── Problem Details + global exception handler ───────────────────────────────
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// ── OpenAPI 3.1 + Swagger UI ─────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RFx Sourcing — Event Service",
        Version = "v1",
        Description = "Owns the sourcing-event lifecycle and the supplier master list."
    });

    opts.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "HS256-signed JWT issued by the Authentication Service."
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "bearerAuth"
            }
        }] = Array.Empty<string>()
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "EventService.xml");
    if (File.Exists(xmlPath))
    {
        opts.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

// ── Pipeline ─────────────────────────────────────────────────────────────────
app.UseExceptionHandler();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(o =>
{
    o.EnrichDiagnosticContext = (diag, ctx) =>
    {
        if (ctx.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var corr))
        {
            diag.Set("CorrelationId", corr?.ToString());
        }
    };
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// OpenAPI document at /api/v1/openapi.json + Swagger UI at /swagger.
// (Swashbuckle 6.x emits OpenAPI 3.0; bump path: an OpenAPI 3.1 surface is
// a non-blocking polish item — the spec is fully consumed by Swagger UI today.)
app.UseSwagger(c =>
{
    c.RouteTemplate = "api/{documentName}/openapi.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api/v1/openapi.json", "Event Service v1");
    c.RoutePrefix = "swagger";
});

// ── Startup tasks ────────────────────────────────────────────────────────────
await app.Services.ApplyMigrationsAsync();
await app.Services.RunSeedAsync();

// ── Graceful shutdown hooks (Ctrl+C, SIGTERM) ────────────────────────────────
var lifetime = app.Lifetime;
lifetime.ApplicationStarted.Register(() =>
{
    _ = StartupBanner.RenderAsync(app);
    Log.Information("Event Service started and listening.");
});
lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Shutdown signal received. Draining in-flight requests...");
});
lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Event Service stopped. Goodbye.");
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Event Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Public partial Program type to make WebApplicationFactory&lt;Program&gt; usable from tests.</summary>
public partial class Program { }

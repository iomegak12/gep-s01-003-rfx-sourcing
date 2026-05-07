using EventService.Mcp.Configuration;
using EventService.Mcp.Handlers;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Prompts;
using EventService.Mcp.Resources;
using EventService.Mcp.Startup;
using EventService.Mcp.Tools;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

builder.Services
    .AddOptions<EventServiceOptions>()
    .Bind(builder.Configuration.GetSection(EventServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<McpServerOptions>()
    .Bind(builder.Configuration.GetSection(McpServerOptions.SectionName));

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<BearerTokenForwardingHandler>();

builder.Services
    .AddHttpClient<IEventServiceClient, EventServiceClient>((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<EventServiceOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenForwardingHandler>();

builder.Services
    .AddHttpClient<IEventServiceHealthClient, EventServiceHealthClient>((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<EventServiceOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

var mcpOptions = builder.Configuration
    .GetSection(McpServerOptions.SectionName)
    .Get<McpServerOptions>() ?? new McpServerOptions();

builder.Services
    .AddMcpServer(o =>
    {
        o.ServerInfo = new() { Name = mcpOptions.ServerName, Version = mcpOptions.ServerVersion };
    })
    .WithHttpTransport(o => o.Stateless = mcpOptions.Stateless)
    .WithTools<SupplierTools>()
    .WithTools<EventTools>()
    .WithTools<LineItemTools>()
    .WithTools<InvitationTools>()
    .WithTools<HealthTools>()
    .WithResources<EventResources>()
    .WithResources<SupplierResources>()
    .WithResources<ReferenceResources>()
    .WithPrompts<EventPrompts>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

app.MapMcp(mcpOptions.EndpointPath);

app.MapGet("/api/v1/health", () => Results.Ok(new
{
    status = "ok",
    service = "event-service-mcp",
    timestamp = DateTimeOffset.UtcNow
}));

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
    StartupBanner.Render(app.Services, app.Configuration));
lifetime.ApplicationStopping.Register(() =>
    StartupBanner.RenderShutdown("yellow", "Shutting down…", "Ctrl+C received, draining in-flight requests."));
lifetime.ApplicationStopped.Register(() =>
    StartupBanner.RenderShutdown("green", "Stopped.", "Event Service MCP exited cleanly."));

app.Run();

public partial class Program;

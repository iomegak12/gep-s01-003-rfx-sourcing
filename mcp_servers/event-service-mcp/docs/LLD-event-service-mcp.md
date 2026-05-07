# LLD — Event Service MCP Server

**Document type:** Low-Level Design
**Subject system:** RFx Sourcing — Event Service MCP Server
**Author:** Solution Architecture
**Status:** Draft v1.0
**Audience:** Developers implementing the MCP server

---

## 1. Purpose & scope

### 1.1 Purpose

This document specifies the low-level design of a new **MCP (Model Context Protocol) server** that exposes the existing **Event Service REST API** to Agentic AI systems. It is detailed enough for a developer to implement the service end-to-end in .NET 9.

### 1.2 In scope (Phase 1)

- A standalone .NET 9 process implementing an MCP server over **Streamable HTTP transport**.
- **1:1 mapping** of every public Event Service REST endpoint to an MCP **Tool** (14 tools total).
- **3 MCP Resources** for read-only data and reference grounding.
- **4 MCP Prompt Templates** covering the event lifecycle (draft, validate, populate, report).
- **Pass-through JWT authentication** — the MCP server forwards the caller-supplied Bearer token to the upstream Event Service without storing credentials.
- Error translation from RFC-7807 `ProblemDetails` to MCP error semantics.
- Structured logging with correlation-ID propagation.
- A three-tier test strategy mirroring the existing Event Service (unit / in-process integration / live-server).

### 1.3 Out of scope (Phase 1)

- Award workflow and BSS (Bid Scoring Service) integration — already deferred upstream.
- Composite "workflow" tools that bundle multiple REST calls.
- Replacement of REST with gRPC / direct DB / message bus (a Phase-2 concern, but the design notes the migration path).
- MCP server-to-client features (sampling, elicitation, root-list change notifications) — not required for this use case; the server runs in **stateless** mode.
- Caller authorisation logic inside the MCP server — the upstream Event Service remains the authoritative authorisation gate.

### 1.4 Glossary

| Term | Definition |
|------|------------|
| **MCP** | Model Context Protocol — open protocol for connecting AI agents to tools, resources, and prompts. |
| **Tool** | A callable function exposed by an MCP server. Used by an LLM/agent to take an action. |
| **Resource** | Read-only data exposed by an MCP server, addressable by URI. |
| **Prompt** | A reusable, parameterised message template exposed by an MCP server. |
| **Streamable HTTP** | The MCP transport variant that runs over ordinary HTTP, suitable for remote/multi-client deployments. |
| **Pass-through auth** | The MCP server holds no credentials; the JWT travels from client → MCP server → upstream service. |

---

## 2. System context

### 2.1 Component diagram (text)

```
+--------------------+      Bearer JWT      +-----------------------+
|  Agentic AI host   |  ------------------> |  Event Service MCP    |
|  (LLM + MCP client)|     (HTTP/MCP)       |  (port 5005)          |
+--------------------+                      +-----------+-----------+
                                                        | Bearer JWT
                                                        | (forwarded)
                                                        v
                                            +-----------+-----------+
                                            |  Event Service REST   |
                                            |  (port 5001)          |
                                            +-----------+-----------+
                                                        |
                                                        v
                                            +-----------------------+
                                            |  SQLite (event_service.db) |
                                            +-----------------------+

                                            +-----------------------+
                                            |  Authentication Svc   |
                                            |  (port 5003) — issues |
                                            |  JWTs to clients      |
                                            +-----------------------+
```

### 2.2 Trust boundaries

| Boundary | Crossed by | Notes |
|----------|------------|-------|
| Agent host ↔ MCP server | HTTP + JWT | MCP server **does not validate** the JWT; it only forwards it. The upstream service rejects bad tokens. |
| MCP server ↔ Event Service | HTTP + JWT | Same JWT signing key & validation rules as today (HS256). |

### 2.3 Deployment model

- Standalone microservice, own container image, own port (**5005**).
- Sits alongside Event Service (5001) and Authentication Service (5003).
- Independently scalable. Stateless — any instance can serve any request.

---

## 3. Solution & project structure

```
EventService.Mcp.sln
├── src/
│   └── EventService.Mcp/
│       ├── EventService.Mcp.csproj
│       ├── Program.cs
│       ├── appsettings.json                 (gitignored)
│       ├── appsettings.Example.json         (committed — template)
│       ├── appsettings.Development.json     (committed)
│       ├── Configuration/
│       │   ├── EventServiceOptions.cs
│       │   └── McpServerOptions.cs
│       ├── HttpClients/
│       │   ├── IEventServiceClient.cs       (typed client interface — Phase-2 seam)
│       │   └── EventServiceClient.cs        (REST implementation; HttpClient based)
│       ├── Handlers/
│       │   └── BearerTokenForwardingHandler.cs
│       ├── Models/
│       │   ├── Requests/                    (mirror upstream request DTOs)
│       │   │   ├── CreateEventRequest.cs
│       │   │   ├── UpdateEventRequest.cs
│       │   │   ├── AddLineItemRequest.cs
│       │   │   └── InviteSupplierRequest.cs
│       │   └── Responses/                   (mirror upstream response DTOs)
│       │       ├── EventResponse.cs
│       │       ├── EventListResponse.cs
│       │       ├── LineItemResponse.cs
│       │       ├── LineItemListResponse.cs
│       │       ├── InvitationResponse.cs
│       │       ├── InvitationListResponse.cs
│       │       ├── SupplierResponse.cs
│       │       ├── SupplierListResponse.cs
│       │       └── EventStatus.cs           (enum)
│       ├── Errors/
│       │   ├── UpstreamException.cs
│       │   └── ProblemDetailsExceptionMapper.cs
│       ├── Tools/
│       │   ├── SupplierTools.cs             (2 tools)
│       │   ├── EventTools.cs                (5 tools)
│       │   ├── LineItemTools.cs             (3 tools)
│       │   ├── InvitationTools.cs           (3 tools)
│       │   └── HealthTools.cs               (1 tool)
│       ├── Resources/
│       │   ├── EventResources.cs            (event://{id})
│       │   ├── SupplierResources.cs         (suppliers://master-list)
│       │   └── ReferenceResources.cs        (reference://event-status)
│       └── Prompts/
│           └── EventPrompts.cs              (4 prompts)
├── tests/
│   └── EventService.Mcp.Tests/
│       ├── EventService.Mcp.Tests.csproj
│       ├── Unit/                            (xUnit + Moq)
│       ├── Integration/                     (WebApplicationFactory + WireMock.Net)
│       └── LiveServer/                      (HttpClient-based, gated by env var)
└── scripts/
    ├── run.ps1
    └── clean.ps1
```

### 3.1 NuGet package references

| Package | Purpose | Version (target) |
|---------|---------|------------------|
| `ModelContextProtocol.AspNetCore` | MCP server SDK + HTTP transport | latest stable (≥ 1.2.x) |
| `Microsoft.Extensions.Hosting` | Generic host | 9.x |
| `Microsoft.Extensions.Http` | `IHttpClientFactory`, named/typed clients, `DelegatingHandler` | 9.x |
| `Microsoft.AspNetCore.Http` (transitive) | `IHttpContextAccessor` | n/a (framework) |
| `Serilog.AspNetCore` | Structured logging, correlation enricher | 8.x |
| `Serilog.Sinks.Console` | JSON console sink | 6.x |
| **Test packages** | | |
| `xunit`, `xunit.runner.visualstudio` | Unit + integration test runner | latest |
| `Moq` | Mocking | latest |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory` for in-process tests | 9.x |
| `WireMock.Net` | Stubs upstream Event Service in integration tests | latest |

> The `ModelContextProtocol.AspNetCore` package transitively pulls in `ModelContextProtocol` and `ModelContextProtocol.Core`, so no separate references are needed.

---

## 4. Configuration

### 4.1 `appsettings.json` (template)

```jsonc
{
  "Mcp": {
    "ServerName": "event-service-mcp",
    "ServerVersion": "1.0.0",
    "EndpointPath": "/mcp",
    "Stateless": true
  },
  "EventService": {
    "BaseUrl": "http://localhost:5001",
    "TimeoutSeconds": 30
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://0.0.0.0:5005" }
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "System.Net.Http.HttpClient": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": { "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact" }
      }
    ]
  }
}
```

### 4.2 Environment variables (override-friendly)

| Variable | Maps to | Notes |
|----------|---------|-------|
| `EVENT_SERVICE_BASE_URL` | `EventService:BaseUrl` | Used in container deploys to point at the real upstream. |
| `MCP_ENDPOINT_PATH` | `Mcp:EndpointPath` | Defaults to `/mcp`. |
| `ASPNETCORE_URLS` | Kestrel binding | E.g. `http://0.0.0.0:5005`. |

### 4.3 Configuration classes

```csharp
// Configuration/EventServiceOptions.cs
public sealed class EventServiceOptions
{
    public const string SectionName = "EventService";
    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}

// Configuration/McpServerOptions.cs
public sealed class McpServerOptions
{
    public const string SectionName = "Mcp";
    public string ServerName { get; init; } = "event-service-mcp";
    public string ServerVersion { get; init; } = "1.0.0";
    public string EndpointPath { get; init; } = "/mcp";
    public bool Stateless { get; init; } = true;
}
```

---

## 5. `Program.cs` wiring

```csharp
using EventService.Mcp.Configuration;
using EventService.Mcp.Handlers;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Prompts;
using EventService.Mcp.Resources;
using EventService.Mcp.Tools;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Logging ----------------------------------------------------------------
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// --- Options ----------------------------------------------------------------
builder.Services
    .AddOptions<EventServiceOptions>()
    .Bind(builder.Configuration.GetSection(EventServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<McpServerOptions>()
    .Bind(builder.Configuration.GetSection(McpServerOptions.SectionName));

// --- HTTP context accessor (needed for pass-through auth) -------------------
builder.Services.AddHttpContextAccessor();

// --- Bearer-token forwarding handler ----------------------------------------
builder.Services.AddTransient<BearerTokenForwardingHandler>();

// --- Typed HttpClient for upstream Event Service ----------------------------
builder.Services
    .AddHttpClient<IEventServiceClient, EventServiceClient>((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<EventServiceOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BearerTokenForwardingHandler>();

// --- MCP server -------------------------------------------------------------
builder.Services
    .AddMcpServer(o =>
    {
        var opts = builder.Configuration
            .GetSection(McpServerOptions.SectionName)
            .Get<McpServerOptions>() ?? new McpServerOptions();
        o.ServerInfo = new() { Name = opts.ServerName, Version = opts.ServerVersion };
    })
    .WithHttpTransport(o => o.Stateless = true)
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

// MCP endpoint — defaults to /mcp; override via Mcp:EndpointPath
var mcpPath = app.Services.GetRequiredService<IOptions<McpServerOptions>>().Value.EndpointPath;
app.MapMcp(mcpPath);

// Liveness probe (does NOT require auth, mirrors upstream pattern)
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }));

app.Run();
```

### 5.1 Key wiring decisions

| Decision | Rationale |
|----------|-----------|
| `WithHttpTransport(o => o.Stateless = true)` | We do not need server-to-client requests (sampling, elicitation). Stateless mode supports horizontal scaling. |
| `AddHttpContextAccessor()` | Required for the bearer-token handler to read the inbound `Authorization` header. |
| `AddHttpClient<IEventServiceClient, EventServiceClient>` | Typed client. Provides a Phase-2 seam: a future `GrpcEventServiceClient` or `MessagingEventServiceClient` can implement the same interface. |
| Tool/Resource/Prompt classes registered explicitly via `WithTools<T>` etc. (instead of `WithToolsFromAssembly`) | Explicit registration is easier to audit, version, and feature-flag. |

---

## 6. Authentication — pass-through pattern

### 6.1 Requirements

- The MCP server **must not** store, cache, or refresh credentials.
- The MCP server **must** read the `Authorization` header from the inbound MCP HTTP request and forward it verbatim on the upstream call.
- If the inbound request has no `Authorization` header, the MCP server should **fail fast** with a clear MCP error rather than letting the upstream return a confusing 401.

### 6.2 `BearerTokenForwardingHandler`

```csharp
// Handlers/BearerTokenForwardingHandler.cs
using System.Net.Http.Headers;

public sealed class BearerTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BearerTokenForwardingHandler> _logger;

    public BearerTokenForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<BearerTokenForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        var authHeader = ctx?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader))
        {
            // Surface as a tool-level error via McpException further upstream.
            throw new UpstreamException(
                StatusCode: 401,
                Title: "Unauthorized",
                Detail: "Missing Authorization header on the MCP request.");
        }

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new UpstreamException(
                StatusCode: 401,
                Title: "Unauthorized",
                Detail: "Authorization header must use the Bearer scheme.");
        }

        request.Headers.Authorization =
            AuthenticationHeaderValue.Parse(authHeader);

        // Optional: forward correlation ID if present
        if (ctx?.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) == true)
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", cid.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
```

### 6.3 Why a `DelegatingHandler`?

- It runs inside the request scope, so `IHttpContextAccessor.HttpContext` is reliable.
- Tool methods stay clean — they call `_eventServiceClient.CreateEventAsync(...)` and the handler injects the token automatically.
- The same pattern works for the eventual Phase-2 transport (e.g. a gRPC client could use a `CallCredentials` adapter that reads the same `IHttpContextAccessor`).

---

## 7. Upstream HttpClient — `IEventServiceClient`

### 7.1 Interface (the Phase-2 seam)

```csharp
// HttpClients/IEventServiceClient.cs
public interface IEventServiceClient
{
    // Suppliers
    Task<SupplierListResponse> ListSuppliersAsync(int limit, int offset, bool activeOnly, CancellationToken ct);
    Task<SupplierResponse> GetSupplierAsync(Guid id, CancellationToken ct);

    // Events
    Task<EventResponse> CreateEventAsync(CreateEventRequest req, CancellationToken ct);
    Task<EventListResponse> ListEventsAsync(int limit, int offset, CancellationToken ct);
    Task<EventResponse> GetEventAsync(Guid id, CancellationToken ct);
    Task<EventResponse> UpdateEventAsync(Guid id, UpdateEventRequest req, CancellationToken ct);
    Task<EventResponse> PublishEventAsync(Guid id, CancellationToken ct);

    // Line items
    Task<LineItemResponse> AddLineItemAsync(Guid eventId, AddLineItemRequest req, CancellationToken ct);
    Task<LineItemListResponse> ListLineItemsAsync(Guid eventId, CancellationToken ct);
    Task DeleteLineItemAsync(Guid eventId, Guid itemId, CancellationToken ct);

    // Invitations
    Task<InvitationResponse> InviteSupplierAsync(Guid eventId, InviteSupplierRequest req, CancellationToken ct);
    Task<InvitationListResponse> ListInvitationsAsync(Guid eventId, CancellationToken ct);
    Task RevokeInvitationAsync(Guid eventId, Guid supplierId, CancellationToken ct);

    // Health
    Task<HealthResponse> GetHealthAsync(CancellationToken ct);
}
```

### 7.2 Implementation pattern (excerpt — repeat for each method)

```csharp
// HttpClients/EventServiceClient.cs (excerpt)
public sealed class EventServiceClient : IEventServiceClient
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly ILogger<EventServiceClient> _logger;

    public EventServiceClient(HttpClient http, ILogger<EventServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<EventResponse> CreateEventAsync(
        CreateEventRequest req, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync("/api/v1/events", req, JsonOpts, ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<EventResponse>(JsonOpts, ct))!;
    }

    // ... other methods follow the same shape
}
```

---

## 8. Error translation strategy

### 8.1 `UpstreamException` & `ProblemDetailsExceptionMapper`

```csharp
// Errors/UpstreamException.cs
public sealed class UpstreamException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public string? Detail { get; }

    public UpstreamException(int StatusCode, string Title, string? Detail = null)
        : base($"{StatusCode} {Title}: {Detail}")
    {
        this.StatusCode = StatusCode;
        this.Title = Title;
        this.Detail = Detail;
    }
}

// Errors/ProblemDetailsExceptionMapper.cs
public static class ProblemDetailsExceptionMapper
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;

        // Try parse RFC-7807 ProblemDetails; fall back to status text otherwise.
        ProblemDetails? pd = null;
        try
        {
            if (resp.Content.Headers.ContentType?.MediaType?.Contains("problem+json") == true ||
                resp.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
            {
                pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            }
        }
        catch { /* ignore parse errors */ }

        throw new UpstreamException(
            StatusCode: (int)resp.StatusCode,
            Title:      pd?.Title  ?? resp.ReasonPhrase ?? "Upstream error",
            Detail:     pd?.Detail);
    }
}

public sealed class ProblemDetails
{
    public string? Type   { get; set; }
    public string? Title  { get; set; }
    public int?    Status { get; set; }
    public string? Detail { get; set; }
}
```

### 8.2 Mapping `UpstreamException` to MCP errors

In each tool method, wrap upstream calls and translate to `McpException` so the MCP client receives a well-formed error block (`IsError = true`):

```csharp
try
{
    return await _client.CreateEventAsync(req, ct);
}
catch (UpstreamException ex)
{
    throw new McpException(FormatUpstreamMessage(ex));
}

static string FormatUpstreamMessage(UpstreamException ex) => ex.StatusCode switch
{
    400 => $"Validation failed: {ex.Detail ?? ex.Title}",
    401 => "Authentication failed. The provided token is missing, invalid, or expired.",
    403 => "Access denied for the current principal.",
    404 => $"Not found: {ex.Detail ?? ex.Title}",
    409 => $"Operation rejected: {ex.Detail ?? ex.Title}",
    _   => $"Upstream error ({ex.StatusCode} {ex.Title}): {ex.Detail}"
};
```

> **Why `McpException` (not `McpProtocolException`)?** Per the SDK docs, `McpException` is returned to the agent inside a `CallToolResult` with `IsError = true` — the agent **sees** the message and can recover. `McpProtocolException` propagates as a JSON-RPC error and is typically reserved for genuine protocol violations (unknown tool, malformed params).

### 8.3 HTTP status → user-facing message matrix

| Upstream HTTP | MCP behaviour | Message template |
|---------------|---------------|------------------|
| 200 / 201 / 204 | success | (return the parsed DTO) |
| 400 | tool error | `Validation failed: {detail}` |
| 401 | tool error | `Authentication failed. The provided token is missing, invalid, or expired.` |
| 403 | tool error | `Access denied for the current principal.` |
| 404 | tool error | `Not found: {detail}` |
| 409 | tool error | `Operation rejected: {detail}` (covers all gate violations: not-Draft, supplier inactive, already invited, etc.) |
| 5xx / network | tool error | `Upstream error ({status} {title}): {detail}` |
| Cancellation | re-throws `OperationCanceledException` | (per SDK contract) |

---

## 9. Tool catalog (14 tools)

> **Naming convention:** snake_case, verb-first. The C# method may use PascalCase; the SDK derives the wire name automatically (or override with `[McpServerTool(Name = "...")]`).

### 9.1 Suppliers (2)

#### 9.1.1 `list_suppliers`

| Field | Value |
|-------|-------|
| Maps to | `GET /api/v1/suppliers?limit&offset&activeOnly` |
| Description | Returns a paginated list of suppliers from the organisation-wide master list. |
| Parameters | `limit:int=20`, `offset:int=0`, `activeOnly:bool=true` |
| Returns | `SupplierListResponse` (JSON) |
| Errors | 401, 5xx |

```csharp
[McpServerToolType]
public class SupplierTools
{
    private readonly IEventServiceClient _client;
    public SupplierTools(IEventServiceClient client) => _client = client;

    [McpServerTool(Name = "list_suppliers")]
    [Description("Returns a paginated list of suppliers from the organisation-wide master list.")]
    public async Task<SupplierListResponse> ListSuppliers(
        [Description("Maximum number of suppliers to return (1–100). Defaults to 20.")] int limit = 20,
        [Description("Zero-based offset for pagination. Defaults to 0.")] int offset = 0,
        [Description("If true, only active suppliers are returned. Defaults to true.")] bool activeOnly = true,
        CancellationToken ct = default)
    {
        try { return await _client.ListSuppliersAsync(limit, offset, activeOnly, ct); }
        catch (UpstreamException ex) { throw new McpException(FormatUpstreamMessage(ex)); }
    }
    // ...
}
```

#### 9.1.2 `get_supplier`

| Field | Value |
|-------|-------|
| Maps to | `GET /api/v1/suppliers/{id}` |
| Description | Returns a single supplier by ID. |
| Parameters | `id:Guid` (required) |
| Returns | `SupplierResponse` |
| Errors | 401, 404, 5xx |

---

### 9.2 Events (5)

#### 9.2.1 `create_event`

| Field | Value |
|-------|-------|
| Maps to | `POST /api/v1/events` |
| Description | Creates a new sourcing event in **Draft** status. |
| Parameters | `request: CreateEventRequest` |
| Returns | `EventResponse` |
| Errors | 400, 401, 5xx |

```csharp
[McpServerTool(Name = "create_event")]
[Description("Creates a new sourcing event in Draft status. Returns the created event including its generated ID.")]
public async Task<EventResponse> CreateEvent(
    [Description("Title, description, category, currency, and response deadline (UTC, ISO-8601).")]
    CreateEventRequest request,
    CancellationToken ct = default)
{
    try { return await _client.CreateEventAsync(request, ct); }
    catch (UpstreamException ex) { throw new McpException(FormatUpstreamMessage(ex)); }
}
```

#### 9.2.2 `list_events`

| Field | Value |
|-------|-------|
| Maps to | `GET /api/v1/events?limit&offset` |
| Parameters | `limit:int=20`, `offset:int=0` |
| Returns | `EventListResponse` |

#### 9.2.3 `get_event`

| Field | Value |
|-------|-------|
| Maps to | `GET /api/v1/events/{id}` |
| Parameters | `id:Guid` |
| Returns | `EventResponse` |
| Errors | 401, 404 |

#### 9.2.4 `update_event`

| Field | Value |
|-------|-------|
| Maps to | `PUT /api/v1/events/{id}` |
| Description | Updates a Draft event. Returns 409 if the event is not in Draft status. |
| Parameters | `id:Guid`, `request: UpdateEventRequest` |
| Returns | `EventResponse` |
| Errors | 400, 401, 404, **409 (not Draft)** |

#### 9.2.5 `publish_event`

| Field | Value |
|-------|-------|
| Maps to | `POST /api/v1/events/{id}/publish` |
| Description | Publishes a Draft event. All local gates must pass: deadline in the future, ≥1 line item, ≥1 invited supplier. |
| Parameters | `id:Guid` |
| Returns | `EventResponse` (status will be `Published`) |
| Errors | 401, 404, **409 (any gate violation)** |

> Tool description should explicitly enumerate the gates so the agent can self-correct: *"Local publish gates: event must be Draft; responseDeadlineUtc must be in the future; at least one line item must exist; at least one supplier must be invited."*

---

### 9.3 Line items (3)

#### 9.3.1 `add_line_item`

| Field | Value |
|-------|-------|
| Maps to | `POST /api/v1/events/{eventId}/line-items` |
| Parameters | `eventId:Guid`, `request: AddLineItemRequest` (`description`, `quantity`, `unitPrice`) |
| Returns | `LineItemResponse` |
| Errors | 400, 401, 404, **409 (event not Draft)** |

#### 9.3.2 `list_line_items`

| Field | Value |
|-------|-------|
| Maps to | `GET /api/v1/events/{eventId}/line-items` |
| Parameters | `eventId:Guid` |
| Returns | `LineItemListResponse` |

#### 9.3.3 `delete_line_item`

| Field | Value |
|-------|-------|
| Maps to | `DELETE /api/v1/events/{eventId}/line-items/{itemId}` |
| Parameters | `eventId:Guid`, `itemId:Guid` |
| Returns | `string` (confirmation message — no body upstream) |
| Errors | 401, 404, **409 (event not Draft)** |

---

### 9.4 Invitations (3)

#### 9.4.1 `invite_supplier`

| Field | Value |
|-------|-------|
| Maps to | `POST /api/v1/events/{eventId}/invitations` |
| Description | Invites an active supplier to a Draft event. |
| Parameters | `eventId:Guid`, `request: InviteSupplierRequest` (`supplierId`) |
| Returns | `InvitationResponse` |
| Errors | 400, 401, 404, **409 (not Draft / supplier inactive / already invited)** |

#### 9.4.2 `list_invitations`

| Field | Value |
|-------|-------|
| Maps to | `GET /api/v1/events/{eventId}/invitations` |
| Parameters | `eventId:Guid` |
| Returns | `InvitationListResponse` |

#### 9.4.3 `revoke_invitation`

| Field | Value |
|-------|-------|
| Maps to | `DELETE /api/v1/events/{eventId}/invitations/{supplierId}` |
| Parameters | `eventId:Guid`, `supplierId:Guid` |
| Returns | `string` confirmation |
| Errors | 401, 404, **409 (event not Draft)** |

---

### 9.5 Infrastructure (1)

#### 9.5.1 `health_check`

| Field | Value |
|-------|-------|
| Maps to | `GET /api/v1/health` |
| Description | Liveness probe for the upstream Event Service. Does not require authentication. |
| Parameters | (none) |
| Returns | `HealthResponse` |

> **Note:** `health_check` is the only tool that does **not** need a Bearer token. The forwarding handler still attaches one if present, but the upstream allows anonymous; the tool itself should not throw on a missing header. Implement by using a separate named `HttpClient` or a flag on the request to skip the handler.

---

## 10. Resource catalog (3 resources)

### 10.1 `event://{id}` — live event detail (templated)

```csharp
[McpServerResourceType]
public class EventResources
{
    private readonly IEventServiceClient _client;
    public EventResources(IEventServiceClient client) => _client = client;

    [McpServerResource(
        UriTemplate = "event://{id}",
        Name        = "Sourcing Event",
        MimeType    = "application/json")]
    [Description("Returns a single sourcing event (header, status, deadline, currency, category) by ID.")]
    public async Task<ResourceContents> GetEvent(string id, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new McpException($"Invalid event ID: {id}");

        try
        {
            var ev = await _client.GetEventAsync(guid, ct);
            return new TextResourceContents
            {
                Uri      = $"event://{id}",
                MimeType = "application/json",
                Text     = JsonSerializer.Serialize(ev, JsonOpts)
            };
        }
        catch (UpstreamException ex) when (ex.StatusCode == 404)
        {
            throw new McpException($"Event not found: {id}");
        }
        catch (UpstreamException ex)
        {
            throw new McpException(FormatUpstreamMessage(ex));
        }
    }
}
```

| Field | Value |
|-------|-------|
| URI template | `event://{id}` |
| MIME type | `application/json` |
| Backing call | `GetEventAsync(id)` |
| Error contract | 404 → "Event not found: {id}" |

### 10.2 `suppliers://master-list` — full active supplier list

```csharp
[McpServerResource(
    UriTemplate = "suppliers://master-list",
    Name        = "Supplier Master List",
    MimeType    = "application/json")]
[Description("Returns the full list of active suppliers from the organisation-wide master list. Paginated; the resource fetches up to 100 at a time.")]
public async Task<ResourceContents> GetMasterList(CancellationToken ct = default)
{
    // Page through up to a reasonable upper bound (e.g. 1000).
    var all = new List<SupplierResponse>();
    int offset = 0, page = 100;
    while (all.Count < 1000)
    {
        var resp = await _client.ListSuppliersAsync(page, offset, activeOnly: true, ct);
        all.AddRange(resp.Items);
        if (resp.Items.Count < page) break;
        offset += page;
    }
    return new TextResourceContents
    {
        Uri      = "suppliers://master-list",
        MimeType = "application/json",
        Text     = JsonSerializer.Serialize(new { count = all.Count, items = all }, JsonOpts)
    };
}
```

| Field | Value |
|-------|-------|
| URI | `suppliers://master-list` (direct, not templated) |
| MIME type | `application/json` |
| Pagination | Server-side, transparent to the agent. Hard cap at 1000 to bound payload. |
| Cache hint | Could add HTTP `ETag` support in Phase 2; not in scope here. |

### 10.3 `reference://event-status` — static reference (state machine + publish gates)

```csharp
[McpServerResource(
    UriTemplate = "reference://event-status",
    Name        = "Event Status Reference",
    MimeType    = "application/json")]
[Description("Static reference: EventStatus enum values, valid transitions, and the local publish-gate checklist.")]
public static ResourceContents GetEventStatusReference()
{
    var doc = new
    {
        statuses = new[] { "Draft", "Published" },
        transitions = new[]
        {
            new { from = "Draft", to = "Published", trigger = "publish_event tool" }
        },
        publishGates = new[]
        {
            "Event must be in Draft status",
            "responseDeadlineUtc must be strictly in the future",
            "At least one line item must exist",
            "At least one supplier must be invited"
        },
        notEnforcedYet = new[]
        {
            "BSS scoring-criteria gate (deferred — Phase 6 of impl plan)"
        }
    };
    return new TextResourceContents
    {
        Uri      = "reference://event-status",
        MimeType = "application/json",
        Text     = JsonSerializer.Serialize(doc, JsonOpts)
    };
}
```

| Field | Value |
|-------|-------|
| URI | `reference://event-status` |
| MIME type | `application/json` |
| Backing call | None — static, in-process. |
| Use case | Lets the agent ground its understanding of the lifecycle without trial-and-error tool calls. |

---

## 11. Prompt catalog (4 prompts)

```csharp
[McpServerPromptType]
public class EventPrompts
{
    [McpServerPrompt(Name = "draft_sourcing_event")]
    [Description("Turns a free-text sourcing brief into a concrete CreateEventRequest payload plus suggested line items.")]
    public static IEnumerable<ChatMessage> DraftSourcingEvent(
        [Description("Free-text sourcing brief, e.g. 'I need to source 50 enterprise laptops by August 15.'")]
        string brief)
    {
        const string system =
            "You are a procurement assistant operating against the RFx Sourcing Event Service. " +
            "Your job is to turn a free-text sourcing brief into a structured payload usable by the create_event tool, " +
            "plus one or more line items usable by the add_line_item tool. " +
            "Rules: " +
            "(1) responseDeadlineUtc MUST be ISO-8601 UTC and strictly in the future. " +
            "(2) currency MUST be a valid ISO-4217 code; default to INR if not stated. " +
            "(3) category MUST be a single short token (e.g. 'IT', 'Logistics', 'Office'). " +
            "(4) For each distinct item in the brief, propose a line item with description, quantity, and a reasonable unitPrice estimate (mark estimate=true). " +
            "Output a single JSON object with two top-level keys: 'event' and 'lineItems'.";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Brief:\n{brief}")
        };
    }

    [McpServerPrompt(Name = "pre_publish_readiness_check")]
    [Description("Walks through the local publish gates for a given event ID and reports what is missing.")]
    public static IEnumerable<ChatMessage> PrePublishReadinessCheck(
        [Description("The event ID to check.")] string eventId)
    {
        const string system =
            "You are validating whether a sourcing event is ready to publish. " +
            "Use the available MCP tools and resources in this exact order: " +
            "(1) Read the resource reference://event-status to recall the gates. " +
            "(2) Call get_event with the supplied ID. " +
            "(3) Call list_line_items with the same ID. " +
            "(4) Call list_invitations with the same ID. " +
            "Then produce a concise checklist: for each of the four gates, mark PASS or FAIL with a one-line reason. " +
            "If all four pass, recommend calling publish_event. If any fail, list the exact remediation steps.";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Event ID: {eventId}")
        };
    }

    [McpServerPrompt(Name = "supplier_shortlist")]
    [Description("Suggests suppliers from the master list that match a category and free-text criteria.")]
    public static IEnumerable<ChatMessage> SupplierShortlist(
        [Description("The sourcing category, e.g. 'IT' or 'Logistics'.")] string category,
        [Description("Free-text criteria, e.g. 'GST-registered, India, prior cloud experience'.")] string criteria)
    {
        const string system =
            "You are recommending suppliers for an upcoming sourcing event. " +
            "Procedure: " +
            "(1) Read the resource suppliers://master-list to obtain the candidate set. " +
            "(2) Filter to active suppliers whose attributes match the category and criteria. " +
            "(3) Return up to 5 suppliers, ranked, with a one-line justification each. " +
            "(4) For each shortlisted supplier, include the supplierId so the user can pass it to invite_supplier. " +
            "Be conservative: do NOT invent supplier capabilities not present in the master-list payload.";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Category: {category}\nCriteria: {criteria}")
        };
    }

    [McpServerPrompt(Name = "event_status_summary")]
    [Description("Generates a stakeholder-friendly status summary for a given event.")]
    public static IEnumerable<ChatMessage> EventStatusSummary(
        [Description("The event ID to summarise.")] string eventId,
        [Description("Audience — 'executive' for a 3-line summary, 'operational' for a detailed breakdown.")]
        string audience = "executive")
    {
        const string system =
            "You are producing a status summary for a sourcing event. " +
            "Procedure: " +
            "(1) Call get_event for the supplied ID. " +
            "(2) Call list_line_items and list_invitations. " +
            "(3) Compute days-to-deadline from responseDeadlineUtc and 'today' (UTC). " +
            "(4) Render the summary in the requested format: " +
            "    - 'executive': three lines — status, totals (line items / invitees), days to deadline. " +
            "    - 'operational': bullet list with status, deadline, line-item count and total estimated value, " +
            "      invitee count with names, and any open risks (e.g. deadline within 7 days, no invitations).";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Event ID: {eventId}\nAudience: {audience}")
        };
    }
}
```

### 11.1 Prompt summary table

| Prompt | Args | Lifecycle phase | Tools/resources it nudges the agent to use |
|--------|------|-----------------|---------------------------------------------|
| `draft_sourcing_event` | `brief:string` | Draft | (none — produces structured input) |
| `pre_publish_readiness_check` | `eventId:string` | Validate | `reference://event-status`, `get_event`, `list_line_items`, `list_invitations` |
| `supplier_shortlist` | `category:string`, `criteria:string` | Populate | `suppliers://master-list` |
| `event_status_summary` | `eventId:string`, `audience:string` | Report | `get_event`, `list_line_items`, `list_invitations` |

---

## 12. Logging, correlation & observability

### 12.1 Correlation ID

- Inbound MCP HTTP request → if header `X-Correlation-Id` is present, use it; otherwise generate a new GUID.
- Pushed into Serilog `LogContext` for the full lifetime of the MCP request.
- `BearerTokenForwardingHandler` adds the same header to the upstream HTTP call so the existing Event Service correlation enricher links the two log streams.

### 12.2 What gets logged

| Event | Level | Fields |
|-------|-------|--------|
| MCP request received | `Information` | path, sessionId, correlationId, toolName/resourceUri/promptName |
| Upstream call started | `Debug` | method, path, correlationId |
| Upstream call completed | `Information` | statusCode, durationMs, correlationId |
| Upstream call failed | `Warning` (4xx) / `Error` (5xx) | statusCode, problemDetailTitle, correlationId |
| Tool error returned to agent | `Information` | toolName, message |
| Unhandled exception | `Error` | exception, correlationId |

### 12.3 What must NOT be logged

- The full `Authorization` header value.
- Request bodies that may contain commercially sensitive information (line-item unit prices, supplier names) — log shapes/sizes only, redact values.

---

## 13. Test strategy

Mirrors the existing Event Service approach.

### 13.1 Unit tests (`tests/.../Unit`)

- `BearerTokenForwardingHandlerTests` — verify header pass-through, missing/invalid header throws `UpstreamException`, correlation header propagated.
- `ProblemDetailsExceptionMapperTests` — verify mapping of 400/401/404/409/500 responses, with and without RFC-7807 bodies.
- `EventToolsTests`, `SupplierToolsTests`, etc. — mock `IEventServiceClient`, assert that `UpstreamException` is translated to `McpException` with the expected message, and that success paths return the DTO unchanged.
- Resource handler tests (URL parsing, 404 → `McpException`).
- Prompt method tests (correct number of messages, role assignment, parameter substitution).

### 13.2 In-process integration tests (`tests/.../Integration`)

- Use `WebApplicationFactory<Program>` to spin up the MCP server in-memory.
- Stub the upstream Event Service with **WireMock.Net** (or a custom `HttpMessageHandler`).
- Use the **MCP C# SDK client** (`McpClient` over `StreamableHttpClientTransport`) to drive the server end-to-end:
  - `ListToolsAsync` returns 14 tools.
  - `ListResourcesAsync` returns 2 direct + 1 templated.
  - `ListPromptsAsync` returns 4 prompts.
  - `CallToolAsync("create_event", …)` with a valid Bearer triggers the WireMock stub and returns the expected `EventResponse`.
  - Calling without an `Authorization` header → `IsError = true`, message contains *"Missing Authorization header"*.
  - WireMock stubbed to return 409 → `IsError = true`, message contains *"Operation rejected"*.

### 13.3 Live-server tests (`tests/.../LiveServer`)

- Black-box tests against a running MCP server **and** running Event Service.
- Gated by env vars: `MCP_SERVER_BASE_URL` (default `http://localhost:5005`), `EVENT_SERVICE_BASE_URL`, `TEST_BUYER_USERNAME`, `TEST_BUYER_PASSWORD`.
- If either server is unreachable, tests are **Skipped**, not Failed.
- Smoke-test the happy path: login → list_suppliers → create_event → add_line_item → invite_supplier → publish_event → get_event (assert status = Published).

### 13.4 Acceptance criteria

| # | Criterion |
|---|-----------|
| 1 | All 14 tools, 3 resources, 4 prompts are discoverable via the MCP listing endpoints. |
| 2 | A valid Bearer token flows through the MCP server to the upstream service unchanged. |
| 3 | A missing/invalid Bearer is returned as a structured tool error (not a 500). |
| 4 | All upstream 409 responses surface to the agent as `IsError = true` with the upstream `Detail` text. |
| 5 | Correlation IDs link MCP-server log lines to Event-Service log lines for the same logical request. |
| 6 | Unit + integration test pass rate ≥ 95%. Live-server suite passes when both services are running. |

---

## 14. Phase-2 migration notes (REST → other transports)

When the business decides to retire the REST surface (e.g. in favour of gRPC, a direct DB read path, or a message-bus command channel), the following changes are mechanical:

| Component | Phase-1 (now) | Phase-2 (later) | Effort |
|-----------|---------------|-----------------|--------|
| `IEventServiceClient` | unchanged interface | unchanged interface | **0** — the seam is the interface. |
| `EventServiceClient` (REST) | retire or keep behind a feature flag | replace with `GrpcEventServiceClient` / `DirectDbEventServiceClient` / `BusEventServiceClient` | medium — one new class per endpoint family. |
| `BearerTokenForwardingHandler` | DelegatingHandler on `HttpClient` | move auth into the new transport's auth adapter (e.g. gRPC `CallCredentials`) | small. |
| `ProblemDetailsExceptionMapper` | parses RFC-7807 from HTTP | replace with transport-specific error mapper, but the resulting `UpstreamException` shape stays the same | small. |
| Tools / Resources / Prompts | **no changes** | **no changes** | **0** — the agent contract is preserved. |
| DTOs | mirror OpenAPI | switch to gRPC-generated types or domain types — keep the wire shape the agent sees identical | small to medium. |

> The single most important design choice for Phase-2 is keeping `IEventServiceClient` stable and pure — no `HttpRequestMessage` types leaking through, no REST status codes in method signatures. The current draft already enforces this.

---

## 15. Open questions / future enhancements

1. **Caching & ETags.** `suppliers://master-list` and `reference://event-status` are good candidates for client-side caching. Phase-2 could add ETag-driven conditional reads.
2. **Server-side observability.** Add OpenTelemetry tracing alongside Serilog, exporting to the same collector as Event Service.
3. **Rate limiting.** Per-token rate limits to protect the upstream from runaway agents. Could be implemented with `Microsoft.AspNetCore.RateLimiting` middleware on `/mcp`.
4. **Tool list filtering by role.** When BSS comes online, an `award_event` tool will appear; we may want to expose it only to certain principals via a custom tool filter.
5. **Streaming results.** Some list operations could benefit from streamed responses (`IAsyncEnumerable`) if the master list grows large.
6. **MCP elicitation.** If we move to stateful mode, `pre_publish_readiness_check` could elicit missing fields from the user instead of just reporting them.

---

## Appendix A — DTO mirrors

The DTOs under `Models/Requests` and `Models/Responses` are **structural mirrors** of the upstream OpenAPI schemas. Recommended approach:

- **Hand-author** with `record` types for clarity and immutability.
- Alternatively, generate with **NSwag** or **Kiota** from `/api/v1/openapi.json` at build time (`dotnet nswag run`). Generated types must live in a separate folder (e.g. `Models/Generated/`) and be wrapped/aliased in hand-authored types if they leak transport concerns.

Example (hand-authored):

```csharp
public sealed record CreateEventRequest(
    string Title,
    string? Description,
    string Category,
    string Currency,
    DateTimeOffset ResponseDeadlineUtc);

public sealed record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string Currency,
    DateTimeOffset ResponseDeadlineUtc,
    EventStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public enum EventStatus { Draft, Published }
```

---

## Appendix B — Quick reference: SDK patterns used

| Concern | SDK API |
|---------|---------|
| Server registration | `builder.Services.AddMcpServer(o => …)` |
| HTTP transport | `.WithHttpTransport(o => o.Stateless = true)` + `app.MapMcp("/mcp")` |
| Tool class registration | `.WithTools<T>()` (explicit) |
| Tool method | `[McpServerToolType]` on class, `[McpServerTool, Description("…")]` on method |
| Tool error (visible to agent) | `throw new McpException("…")` |
| Protocol error (rare) | `throw new McpProtocolException("…", McpErrorCode.InvalidParams)` |
| Resource (templated) | `[McpServerResource(UriTemplate = "x://{id}")]` returning `ResourceContents` |
| Resource (direct) | `[McpServerResource(UriTemplate = "x://fixed")]` returning `ResourceContents` |
| Prompt | `[McpServerPromptType]` + `[McpServerPrompt]`, return `IEnumerable<ChatMessage>` |
| Parameter docs | `[Description("…")]` on each parameter |
| DI in tool/resource/prompt methods | constructor injection on the type, or any DI service as a method parameter |

---

*End of LLD v1.0*

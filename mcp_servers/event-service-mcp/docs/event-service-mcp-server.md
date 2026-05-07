# Plan — Event Service MCP Server (Phase 1, full implementation)

## Context

Solution Architecture has delivered an LLD ([docs/LLD-event-service-mcp.md](LLD-event-service-mcp.md)) and the upstream OpenAPI ([docs/openapi.json](openapi.json)) for a new **.NET 9 MCP server** that wraps the existing Event Service REST API. The MCP server exposes the full Event Service surface to Agentic AI hosts (LLM + MCP client) over Streamable HTTP, in stateless mode, with pass-through JWT auth.

Phase 1 scope is the **full implementation**: 14 tools, 3 resources, 4 prompts, 3 test tiers (unit / in-process integration / live-server). No BSS, no composite workflows, no replacement of REST.

The plan is grounded by:
- The LLD (architecture, DI wiring, error mapping, tool/resource/prompt catalog).
- The OpenAPI (wire shapes, status codes, parameter defaults).
- A read-only audit of the upstream test suite at `services/event-service/tests/EventService.Tests`, which surfaced wire-level facts not visible in OpenAPI alone (camelCase JSON, `JsonStringEnumConverter`, `correlationId` extension on ProblemDetails, `X-Correlation-Id` header propagation, hardcoded HS256 test key).

## Decisions locked in this round

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | Mirror **full OpenAPI EventStatus**: `Draft, Published, Bidding, Scored, Awarded, Closed`. | Future-proof for BSS phases; wire matches upstream verbatim. |
| D2 | `health_check` uses a **separate un-handlered named HttpClient** (no `BearerTokenForwardingHandler`). | Cleanest separation; keeps the auth handler strict for every other call. |
| D3 | Plan covers the **full Phase-1 implementation** end-to-end. | Matches LLD scope. |
| D4 | DTOs are **hand-authored `record` types** under `Models/Requests` and `Models/Responses`, mirroring upstream camelCase wire format via default `JsonSerializerDefaults.Web`. | Simple, reviewable, no codegen toolchain dependency. |
| D5 | Default page sizes follow **OpenAPI**: `list_suppliers` limit=50, `list_events` limit=20. | Source of truth is the upstream contract; the LLD's `limit=20` for suppliers was a drafting error. |
| D6 | `EventResponse` mirrors **all** OpenAPI fields including `version` and `createdByUserId`. | Avoids silent data loss; agents can use them in summaries. |
| D7 | Preserve the upstream `correlationId` extension when surfacing errors to the agent. | Critical for cross-service log linkage, surfaced by the test-suite audit (not in LLD). |

## Project layout (matches LLD §3, with adjustments from D2/D7)

```
EventService.Mcp.sln
├── src/EventService.Mcp/
│   ├── EventService.Mcp.csproj
│   ├── Program.cs
│   ├── appsettings.json / appsettings.Development.json / appsettings.Example.json
│   ├── Configuration/        EventServiceOptions.cs, McpServerOptions.cs
│   ├── HttpClients/          IEventServiceClient.cs, EventServiceClient.cs,
│   │                         IEventServiceHealthClient.cs, EventServiceHealthClient.cs   ← D2
│   ├── Handlers/             BearerTokenForwardingHandler.cs, CorrelationIdMiddleware.cs
│   ├── Models/Requests/      CreateEventRequest, UpdateEventRequest,
│   │                         AddLineItemRequest, InviteSupplierRequest
│   ├── Models/Responses/     EventResponse, EventListResponse, LineItemResponse,
│   │                         LineItemListResponse, InvitationResponse,
│   │                         InvitationListResponse, SupplierResponse,
│   │                         SupplierListResponse, HealthResponse, EventStatus (enum)
│   ├── Errors/               UpstreamException.cs, ProblemDetailsExceptionMapper.cs,
│   │                         ProblemDetails.cs (with correlationId extension – D7)
│   ├── Tools/                SupplierTools, EventTools, LineItemTools,
│   │                         InvitationTools, HealthTools
│   ├── Resources/            EventResources, SupplierResources, ReferenceResources
│   └── Prompts/              EventPrompts
├── tests/EventService.Mcp.Tests/
│   ├── Unit/                 (xUnit + Moq)
│   ├── Integration/          (WebApplicationFactory + WireMock.Net + MCP C# SDK client)
│   └── LiveServer/           (gated; mirrors upstream LiveServerFixture pattern)
└── scripts/                  run.ps1, clean.ps1
```

## Implementation phases

### Phase A — Solution scaffolding & configuration
1. Create `EventService.Mcp.sln`, `src/EventService.Mcp/EventService.Mcp.csproj` (net9.0), and `tests/EventService.Mcp.Tests/EventService.Mcp.Tests.csproj`.
2. Add NuGet packages per LLD §3.1: `ModelContextProtocol.AspNetCore`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Http`, `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Formatting.Compact`. Test packages: `xunit`, `xunit.runner.visualstudio`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, `WireMock.Net`, `Xunit.SkippableFact` (mirror upstream gating idiom).
3. Author `appsettings.json` template (LLD §4.1) and `appsettings.Example.json` (committed); add `appsettings.json` to `.gitignore`.
4. Implement `EventServiceOptions` and `McpServerOptions` (LLD §4.3).

### Phase B — Cross-cutting infrastructure
5. **DTOs** — hand-author all request/response records under `Models/`. Use camelCase via `JsonSerializerDefaults.Web`; enums use `JsonStringEnumConverter` (test-suite audit confirmed enum-as-PascalCase-string on the wire). `EventStatus` includes all six values (D1).
6. **`ProblemDetails` model** — include the `correlationId` extension and any other extension members upstream emits (D7). Implement `ProblemDetailsExceptionMapper.EnsureSuccessAsync` per LLD §8.1, propagating `correlationId` into `UpstreamException`.
7. **`UpstreamException`** — add `CorrelationId` field alongside `StatusCode`/`Title`/`Detail` so tool error messages can include it.
8. **`BearerTokenForwardingHandler`** — implement per LLD §6.2: read `Authorization` from `IHttpContextAccessor`, throw `UpstreamException(401, …)` when missing/non-Bearer, and forward `X-Correlation-Id` if present.
9. **Correlation middleware** — generate/accept `X-Correlation-Id` on inbound MCP requests, push to Serilog `LogContext`, mirror upstream behaviour.

### Phase C — Upstream HttpClient layer
10. Define **`IEventServiceClient`** with all 13 authenticated methods (LLD §7.1) plus a separate **`IEventServiceHealthClient`** for the anonymous health endpoint (D2).
11. Implement `EventServiceClient` (LLD §7.2 pattern, repeated per method). Static `JsonSerializerOptions` with `JsonSerializerDefaults.Web` + `JsonStringEnumConverter`. Every call goes through `ProblemDetailsExceptionMapper.EnsureSuccessAsync`.
12. Implement `EventServiceHealthClient` — uses a second named `HttpClient` registered **without** `BearerTokenForwardingHandler`.
13. **`Program.cs` wiring** (LLD §5):
    - `AddHttpContextAccessor()`
    - `AddTransient<BearerTokenForwardingHandler>()`
    - `AddHttpClient<IEventServiceClient, EventServiceClient>(...).AddHttpMessageHandler<BearerTokenForwardingHandler>()`
    - `AddHttpClient<IEventServiceHealthClient, EventServiceHealthClient>(...)` — **no** handler (D2)
    - `AddMcpServer(...).WithHttpTransport(o => o.Stateless = true).WithTools<…>()` × 5 .`WithResources<…>()` × 3 .`WithPrompts<EventPrompts>()`
    - `app.MapMcp(mcpPath)`
    - `app.MapGet("/api/v1/health", …)` for the MCP server's own liveness probe.

### Phase D — Tools (14)
14. **`SupplierTools`** (2): `list_suppliers`, `get_supplier`. Default `limit=50` (D5).
15. **`EventTools`** (5): `create_event`, `list_events` (default `limit=20`, D5), `get_event`, `update_event`, `publish_event`. `publish_event` description enumerates the four publish gates so the agent can self-correct.
16. **`LineItemTools`** (3): `add_line_item`, `list_line_items`, `delete_line_item` (returns confirmation string).
17. **`InvitationTools`** (3): `invite_supplier`, `list_invitations`, `revoke_invitation`.
18. **`HealthTools`** (1): `health_check` — depends on `IEventServiceHealthClient`, no token requirement (D2). `HealthResponse` mirrors actual upstream payload (`status`, `service`, `timestamp`) — NOT just `{status:"ok"}`.
19. Every tool wraps `UpstreamException` → `McpException` via `FormatUpstreamMessage` (LLD §8.2). Include `correlationId` in the message for 5xx and unexpected codes (D7).

### Phase E — Resources (3) and Prompts (4)
20. **`EventResources`** — `event://{id}` templated resource (LLD §10.1).
21. **`SupplierResources`** — `suppliers://master-list`, paginated server-side, hard-capped at 1000 items (LLD §10.2).
22. **`ReferenceResources`** — `reference://event-status`, static document. Update `statuses` array to match D1 (all six values), but keep `transitions` and `publishGates` Phase-1-accurate (only `Draft → Published` is reachable today; note BSS gates as `notEnforcedYet`).
23. **`EventPrompts`** — implement all four prompts verbatim per LLD §11: `draft_sourcing_event`, `pre_publish_readiness_check`, `supplier_shortlist`, `event_status_summary`.

### Phase F — Tests (three tiers)
24. **Unit (`tests/.../Unit`)**:
    - `BearerTokenForwardingHandlerTests` — header pass-through, missing/invalid header throws, correlation header forwarded.
    - `ProblemDetailsExceptionMapperTests` — 400/401/404/409/500 with and without RFC-7807 bodies; `correlationId` extension preserved (D7).
    - One `*ToolsTests` per tool class — Moq `IEventServiceClient`; assert `UpstreamException` → `McpException` translation and success-path DTO pass-through.
    - `EventResourcesTests` — invalid GUID, 404 → `McpException`, success serialisation.
    - `EventPromptsTests` — message count, role assignment, parameter substitution.
25. **Integration (`tests/.../Integration`)**:
    - `WebApplicationFactory<Program>` host.
    - WireMock.Net stubs upstream Event Service and Authentication Service.
    - Use the **MCP C# SDK client** (`McpClient` over `StreamableHttpClientTransport`) to drive the server end-to-end:
      - `ListToolsAsync` → 14, `ListResourcesAsync` → 2 direct + 1 templated, `ListPromptsAsync` → 4.
      - Happy-path `create_event` with valid Bearer triggers the WireMock stub and returns the expected `EventResponse`.
      - Missing `Authorization` → `IsError=true`, message contains "Missing Authorization header".
      - WireMock stubbed 409 → `IsError=true`, message contains "Operation rejected" and the upstream `Detail`.
26. **LiveServer (`tests/.../LiveServer`)**:
    - Mirror upstream `LiveServerFixture`: env-var-driven base URLs (`MCP_SERVER_BASE_URL`, `EVENT_SERVICE_BASE_URL`), HS256 JWT minted client-side using the same hardcoded test key the upstream tests use (or `EVENT_SERVICE_JWT_KEY` env override). Use `Xunit.SkippableFact` so suites skip — not fail — when servers are down.
    - Smoke-test the lifecycle: list_suppliers → create_event → add_line_item → invite_supplier → publish_event → get_event (assert `status == "Published"`).

## Critical files to create

- [src/EventService.Mcp/Program.cs](../src/EventService.Mcp/Program.cs) — DI + MCP wiring (LLD §5).
- [src/EventService.Mcp/Handlers/BearerTokenForwardingHandler.cs](../src/EventService.Mcp/Handlers/BearerTokenForwardingHandler.cs) — auth pass-through.
- [src/EventService.Mcp/HttpClients/IEventServiceClient.cs](../src/EventService.Mcp/HttpClients/IEventServiceClient.cs) — Phase-2 seam.
- [src/EventService.Mcp/HttpClients/EventServiceClient.cs](../src/EventService.Mcp/HttpClients/EventServiceClient.cs) — REST implementation.
- [src/EventService.Mcp/HttpClients/EventServiceHealthClient.cs](../src/EventService.Mcp/HttpClients/EventServiceHealthClient.cs) — anonymous client (D2).
- [src/EventService.Mcp/Errors/ProblemDetailsExceptionMapper.cs](../src/EventService.Mcp/Errors/ProblemDetailsExceptionMapper.cs) — preserves `correlationId` (D7).
- [src/EventService.Mcp/Tools/](../src/EventService.Mcp/Tools/) — 5 tool classes, 14 methods.
- [src/EventService.Mcp/Resources/](../src/EventService.Mcp/Resources/) — 3 resource classes.
- [src/EventService.Mcp/Prompts/EventPrompts.cs](../src/EventService.Mcp/Prompts/EventPrompts.cs) — 4 prompts.

## Existing upstream patterns to mirror (do not reinvent)

These were confirmed by the test-suite audit and should be replicated for behavioural parity:

- **JWT minting in tests** — upstream uses HS256 with hardcoded key `bsprTMgGLN862rt3oy1Qeh289u0lWn6yOSBijQrDWDTxd44kiV2MsPgRrVd`, issuer `rfx-auth-service`, audience `rfx-sourcing-system`, `sub` claim, `roles=Buyer`, 1h expiry. Reuse this exact pattern in `LiveServerFixture` / integration helpers.
- **Wire format** — camelCase property names (`JsonSerializerDefaults.Web`), enums as PascalCase strings (`JsonStringEnumConverter`).
- **Correlation propagation** — accept inbound `X-Correlation-Id`, generate one if absent, forward verbatim on upstream calls. Surface the upstream `correlationId` extension on errors back to the agent.
- **Skip-not-fail** — `Xunit.SkippableFact` with `Skip.If(!_f.IsAvailable, …)` for live-server tests.

## Verification

End-to-end verification, in order:

1. **Build**: `dotnet build EventService.Mcp.sln -c Debug` — zero warnings, zero errors.
2. **Unit tests**: `dotnet test --filter "FullyQualifiedName~Unit"` — all green.
3. **Integration tests**: `dotnet test --filter "FullyQualifiedName~Integration"` — all green; WireMock stubs assert request shapes and bearer forwarding.
4. **Run the server**: `./scripts/run.ps1` (binds `http://0.0.0.0:5005`, `/mcp` endpoint).
5. **MCP smoke test (manual)** — using the MCP C# SDK client or `mcp-inspector`:
    - `ListToolsAsync` returns 14, `ListResourcesAsync` returns 3, `ListPromptsAsync` returns 4.
    - `CallToolAsync("health_check")` succeeds without a Bearer token.
    - `CallToolAsync("list_suppliers", { limit: 5 })` with a valid Bearer returns supplier items from a running Event Service.
    - `CallToolAsync("create_event", …)` without `Authorization` returns `IsError=true` and the message contains "Missing Authorization header".
6. **Live-server suite**: with Authentication Service (5003), Event Service (5001), and MCP server (5005) all running:
    - `dotnet test --filter "Category=LiveServer"` runs the full lifecycle (login → list suppliers → create event → add line item → invite supplier → publish → get event with `status=Published`).
7. **Logs** — confirm a single `correlationId` value links MCP-server log lines to Event-Service log lines for the same request (acceptance criterion #5 in LLD §13.4).

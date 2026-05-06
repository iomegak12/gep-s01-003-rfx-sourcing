# Changelog

All notable changes to the Event Service are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.8.0] – 2026-05-06

### Added — Phase 8 (Documentation & release polish)

- `README.md` expanded with full API endpoint tables, step-by-step PowerShell sample requests (token → create event → add line items → invite suppliers → publish), running-tests section covering unit/integration/live-server categories, and a reset-local-DB section.
- `CONTRIBUTING.md` rewritten with four-layer architecture rules, a test-category table, a ten-step guide for adding a new endpoint, and a BSS-deferred-endpoints note.
- `TROUBLESHOOTING.md` expanded with sections covering: publish 409 causes table, `CreatedAtAction` workaround rationale, enum-serialisation debugging, live-server test skip vs fail behaviour, `EVENT_SERVICE_JWT_KEY` mismatch for live-server tests.

## [0.7.1] – 2026-05-06

### Added — Phase 7b (Live-server test suite)

- `LiveServerFixture` — `IClassFixture<T>` that probes `GET /api/v1/health` with a 3 s timeout on construction; sets `IsAvailable = false` when the service is not reachable. Base URL read from `EVENT_SERVICE_BASE_URL` env var (default `http://localhost:5001`); JWT signing key from `EVENT_SERVICE_JWT_KEY` env var (default dev key). Exposes a pre-authenticated `HttpClient` and a factory for unauthenticated clients.
- `LiveServerTests` — 11 `[SkippableFact]` tests tagged `[Trait("Category", "LiveServer")]` covering all 11 scenarios from `HappyPathTests` as pure black-box HTTP assertions (no `WebApplicationFactory`, no DI scope, no direct DB access). Tests skip gracefully when the service is not running.
- `Xunit.SkippableFact` 1.4.13 added to the test project for `Skip.If` / `[SkippableFact]` support (xUnit 2.x equivalent of xUnit v3's `Assert.Skip`).

### Changed

- `CONTRIBUTING.md` test section updated with the three test categories and `dotnet test --filter` commands.

## [0.6.0] – 2026-05-06

### Added — Phase 7 (Integration test sweep)

- `EventServiceWebFactory` — `WebApplicationFactory<Program>` subclass that creates an isolated per-test SQLite file in `Path.GetTempPath()`, overrides all Persistence and JWT config via in-memory collection, and deletes the DB on disposal.
- `HappyPathTests` — 11 integration tests using the real ASP.NET Core pipeline + SQLite:
  - `Auth_NoToken_Returns401` — unauthenticated request to a protected route returns 401.
  - `Auth_ValidBuyerToken_Returns200` — HS256-signed JWT with `Buyer` role returns 200.
  - `Health_Returns200_WithoutToken` — liveness endpoint is anonymous.
  - `HappyPath_SeedCreatePublish_EventStatusIsPublished` — end-to-end: fetch seeded suppliers → create event → add 2 line items → invite 2 suppliers → publish → GET event (status=`Published`) → assert audit rows in DB (`EventCreated`, 2×`LineItemAdded`, 2×`InvitationSent`, `EventPublished`).
  - `Publish_NoLineItems_Returns409`, `Publish_NoInvitations_Returns409`, `Publish_AlreadyPublished_Returns409` — publish gate guards.
  - `UpdateEvent_OnPublishedEvent_Returns409`, `AddLineItem_OnPublishedEvent_Returns409` — Draft-only mutation guards.
  - `DuplicateInvitation_Returns409` — idempotent invite guard.
  - `GetEvent_NotFound_Returns404` — 404 Problem Details for unknown ID.

### Fixed

- `CreatedAtAction` replaced with `Created(uri, value)` using explicit paths in `EventsController`, `LineItemsController`, and `InvitationsController` — `CreatedAtAction` threw `InvalidOperationException ("No route matches the supplied values")` in the `TestServer` environment due to route constraint resolution differences.
- `AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` — enum values (e.g. `EventStatus`, `AuditAction`) now serialise as strings in API responses, consistent with REST conventions and needed for integration test assertions.

## [0.5.0] – 2026-05-06

### Added — Phase 5 (Publish vertical slice)

- `PublishAsync` added to `IEventService` / `EventService` — transitions a Draft event to Published after all local gates pass.
- Local publish gates enforced: event must be Draft (→ 409), response deadline must be in the future (→ 409), at least one line item must exist (→ 409), at least one supplier must be invited (→ 409).
- BSS scoring-criteria gate intentionally deferred: code carries `// TODO(BSS-INTEGRATION)` marker at the call site; OpenAPI description on the endpoint documents the omission. See `docs/impl-event-service-plan.md` Phase 6.
- `POST /api/v1/events/{id}/publish` action on `EventsController` — no request body; returns updated `EventResponse` on success.
- `AuditAction.EventPublished` recorded on successful publish.
- No new migration required — status transition uses the existing `events.status` column.
- Startup banner updated with the publish endpoint row.
- xUnit unit tests: happy-path publish, not-Draft guard, past-deadline guard, no-line-items guard, no-invitations guard, and one `[Fact(Skip)]` placeholder for the deferred BSS criteria-count gate (37 total, 36 passed, 1 skipped).

## [0.4.0] – 2026-05-06

### Added — Phase 4 (Invitations vertical slice)

- `EventSupplier` domain entity with composite primary key (`EventId` + `SupplierId`).
- `IInvitationRepository` / `InvitationRepository` — get, list-by-event (with Supplier join for name), count-by-event, add, delete.
- `IInvitationService` / `InvitationService` — enforces all invitation business rules: event must be Draft (→ 409), supplier must exist (→ 404), supplier must be active (→ 409), re-invite is idempotent guard (→ 409).
- `InvitationsController` — `POST /api/v1/events/{id}/invitations` (201), `GET /api/v1/events/{id}/invitations` (200), `DELETE /api/v1/events/{id}/invitations/{supplierId}` (204).
- `InviteSupplierRequestValidator` — ensures `SupplierId` is non-empty.
- EF Core migration `AddInvitations` creating the `event_suppliers` table with composite PK and `ix_event_suppliers_event_id` index.
- `AuditAction.InvitationSent` and `InvitationRevoked` audit entries emitted from the service layer.
- Startup banner updated with Invitations endpoint rows.
- xUnit unit tests covering invite happy-path, all guard conditions (event not found, event not Draft, supplier not found, supplier inactive, duplicate invite), revoke happy-path, revoke not-Draft guard, revoke not-found (31 tests total).

## [0.3.0] – 2026-05-06

### Added — Phase 3 (Events + LineItems vertical slice)

- `Event` domain entity with `Version` field for optimistic concurrency tracking.
- `LineItem` domain entity with FK to `Event` (cascade delete).
- `AuditEvent` domain entity for append-only audit trail.
- `EventStatus` enum: `Draft`, `Published`, `Bidding`, `Scored`, `Awarded`, `Closed`.
- `AuditAction` enum covering all domain transitions (BSS-only actions carry `// TODO(BSS-INTEGRATION)` markers).
- `IEventRepository` / `EventRepository` with list (paginated), get-by-id, add, and update operations.
- `ILineItemRepository` / `LineItemRepository` with list-by-event, count-by-event, add, and delete.
- `IAuditRepository` / `AuditRepository` — append-only write.
- `IAuditService` / `AuditService` — thin facade that creates `AuditEvent` records with UUIDv7 IDs.
- `IEventService` / `EventService` — create, list (limit/offset clamped), get-by-id, update. Update enforces Draft-only guard (→ 409) and increments `Version`.
- `ILineItemService` / `LineItemService` — add, list-by-event, delete. Add/delete enforce Draft-only guard (→ 409).
- `EventsController` — `POST /api/v1/events` (201), `GET /api/v1/events` (paginated), `GET /api/v1/events/{id}`, `PUT /api/v1/events/{id}`.
- `LineItemsController` — `POST /api/v1/events/{id}/line-items` (201), `GET /api/v1/events/{id}/line-items`, `DELETE /api/v1/events/{id}/line-items/{itemId}` (204).
- `CreateEventRequestValidator`, `UpdateEventRequestValidator`, `AddLineItemRequestValidator` — FluentValidation rules per LLD §6 (title 3–200, category 2–80, currency=INR, deadline-in-future, quantity>0, unit-price>0).
- EF Core migration `AddEventsLineItemsAudit` creating `events`, `line_items`, and `audit_events` tables.
- Startup banner updated with Events and LineItems endpoint rows.
- xUnit unit tests covering create/get/list/update event, state-transition guards, audit emission, and line-item add/delete guards (22 tests total).

## [0.2.0] – 2026-05-06

### Added — Phase 2 (Suppliers vertical slice)

- `Supplier` domain entity with case-insensitive unique-name index.
- `ISupplierRepository` / `SupplierRepository` (read + seed-load operations).
- `ISupplierService` / `SupplierService` with limit/offset clamping and `NotFoundException` on missing id.
- `SuppliersController` exposing `GET /api/v1/suppliers` (paginated, `?activeOnly=` filter) and `GET /api/v1/suppliers/{id}`. Both require an authenticated JWT.
- EF Core migration `20260506072932_InitialCreate` creating the `suppliers` table.
- `SeedRunner` + `SupplierSeed` records — six representative suppliers seeded idempotently on startup when `Persistence:SeedDataEnabled=true`.
- `SupplierResponse`, `SupplierListResponse` DTOs.
- xUnit unit tests covering limit clamping, offset clamping, default limit, DTO mapping, and `NotFoundException`.

### Added — Foundation enhancements

- Colourful **Spectre.Console** startup banner with figlet title, settings table (environment, version, bind URL, DB path, DB readiness ping, JWT issuer/audience), API endpoints table, and a Ctrl+C reminder.
- Graceful shutdown logging via `IHostApplicationLifetime` — emits `Shutdown signal received. Draining...` on Ctrl+C and `Goodbye.` once stopped, then flushes Serilog.

### Fixed

- `correlationId` extension on RFC 7807 responses now reads from `HttpContext.Items` (was returning empty when Problem Details middleware fired before the response headers were written).

## [0.1.0] – 2026-05-06

### Added — Phase 1 (Foundation)

- .NET 9 ASP.NET Core Web API project scaffold.
- Standard repository files: `.gitignore`, `LICENSE` (MIT), `README.md`, `CONTRIBUTING.md`, `TROUBLESHOOTING.md`, `CHANGELOG.md`.
- Configuration files: `appsettings.Development.json`, `appsettings.Example.json`. Base `appsettings.json` is gitignored.
- Kestrel listens on `0.0.0.0:5001` (configurable).
- Serilog with JSON console sink and correlation enrichment.
- OpenAPI 3.1 surface via built-in `Microsoft.AspNetCore.OpenApi` and Swagger UI via Swashbuckle.
- JWT Bearer authentication with HS256, locally validated against the Authentication Service's signing key, issuer (`rfx-auth-service`) and audience (`rfx-sourcing-system`). `Buyer` role policy registered.
- Global RFC 7807 Problem Details exception middleware.
- Correlation-ID middleware (`X-Correlation-Id`).
- EF Core 9 + SQLite; configurable database path via `Persistence:DatabasePath`.
- Empty `EventDbContext`; migrations applied on startup.
- `GET /api/v1/health` liveness endpoint (anonymous).
- Run/clean scripts under `scripts/`.
- Test project skeleton under `tests/EventService.Tests/`.

### Deferred — Bid Scoring Service integration

- `POST /events/{id}/award` and BSS-dependent publish gate are intentionally not implemented in this iteration. See `docs/impl-event-service-plan.md` Phase 6.

# Event Service — Phased Implementation Plan

## Context

Ramkumar is building the **Event Service** microservice (one of two services in the RFx Sourcing system). The Bid Scoring Service is being built **in parallel by another team** with no synchronisation guarantee. Therefore this plan delivers Event Service **independently**: every endpoint that requires Bid Scoring Service (BSS) is **omitted in this build** and clearly flagged for incorporation later.

**Stack overrides vs. LLD:** .NET **9** (LLD says .NET 8). Everything else (SQLite, EF Core, Serilog, JWT bearer, FluentValidation, RFC 7807, OpenAPI, modular architecture per HLD §5.3 / LLD §3) stays as documented.

**Authentication mode:** Local JWT validation in Event Service (no Gateway dependency in this build). The Authentication Service at `http://localhost:5003` issues HS256-signed JWTs with `iss=rfx-auth-service`, `aud=rfx-sourcing-system`, `roles` claim. Event Service shares the same `JWT_SIGNING_KEY` and validates locally via `Microsoft.AspNetCore.Authentication.JwtBearer`.

## Locked decisions (from clarification round)

| Topic                | Decision                                                                 |
| -------------------- | ------------------------------------------------------------------------ |
| Phasing style        | Hybrid: foundation phase, then vertical slices per feature.              |
| BSS-dependent endpoints | **Fully omitted** in this build. Flagged in this plan + in code TODOs. |
| JWT auth in dev      | Validate locally; share HS256 signing key + iss/aud with Auth Service.   |
| Test scope per phase | Unit tests per slice; one integration sweep at the end.                  |
| DB init              | EF Core Migrations applied on startup.                                   |
| Standard files       | Inside `services/event-service/` only.                                   |
| Logging              | Serilog with JSON console sink.                                          |
| Health endpoint      | Simple `200 OK` liveness only.                                           |

## Conventions

- **Host/port:** `0.0.0.0:5001` (configurable via `Kestrel:Endpoints` in appsettings).
- **DB path:** configurable via `Persistence:DatabasePath` in appsettings (default `./data/event_service.db`).
- **Base path:** `/api/v1`.
- **OpenAPI:** OpenAPI 3.1 surface at `/api/v1/openapi.json` + Swagger UI at `/swagger`.
- **DI:** every concrete class with behaviour has an interface; registered in `Program.cs` via DI container.
- **Docs:** XML doc comments on Controllers, Services, Repositories, Infrastructure helpers. Domain entities/DTOs/enums skipped per directive.
- **Config files:**
  - `appsettings.json` — **gitignored** (per directive).
  - `appsettings.Development.json` — committed (developer defaults).
  - `appsettings.Example.json` — committed (template buyers/reviewers copy to `appsettings.json`).
  - `appsettings.Staging.json`, `appsettings.Production.json` — gitignored alongside `appsettings.json`.
- **Standard files:** `.gitignore`, `LICENSE` (MIT), `README.md`, `CONTRIBUTING.md`, `TROUBLESHOOTING.md`, `CHANGELOG.md` — all under `services/event-service/`.

## Project layout

Per LLD §2 (kept as-is):

```
services/event-service/
├── EventService.csproj
├── EventService.sln
├── Program.cs
├── appsettings.Development.json
├── appsettings.Example.json
├── .gitignore  LICENSE  README.md  CONTRIBUTING.md  TROUBLESHOOTING.md  CHANGELOG.md
├── Controllers/   Services/   Repositories/   Domain/   Infrastructure/   Migrations/
├── data/                       (SQLite file, gitignored)
├── scripts/                    (run.ps1, clean.ps1)
└── tests/EventService.Tests/   (xUnit + Moq)
```

---

## Phase 1 — Foundation

**Goal:** A runnable, empty-but-wired service with all cross-cutting concerns in place. No business endpoints yet.

**Deliverables**
- `EventService.csproj` (`net9.0`, nullable + implicit usings on).
- Standard files: `.gitignore` (excludes `appsettings.json`, `appsettings.Staging.json`, `appsettings.Production.json`, `bin/`, `obj/`, `data/`, `*.db`), `LICENSE` (MIT, holder = Ramkumar / org), `README.md` (run instructions), `CONTRIBUTING.md`, `TROUBLESHOOTING.md`, `CHANGELOG.md` (Keep-a-Changelog format, v0.1.0 = "Initial scaffold").
- Configuration files: `appsettings.Development.json`, `appsettings.Example.json` (template). `appsettings.json` mentioned in README as required-to-create.
- `Program.cs` wires: Kestrel on `0.0.0.0:5001`, Serilog (JSON console + correlation enricher), OpenAPI 3.1 (built-in `Microsoft.AspNetCore.OpenApi` + Swashbuckle.AspNetCore for Swagger UI), JWT bearer middleware, Problem Details middleware (RFC 7807), correlation-ID middleware, FluentValidation, EF Core SQLite, DI registrations.
- `Infrastructure/Auth/JwtBearerOptionsSetup.cs` — binds `Jwt:SigningKey`, `Jwt:Issuer`, `Jwt:Audience` from config; `Buyer` role policy registered.
- `Infrastructure/Errors/ProblemDetailsMiddleware.cs` — maps `NotFoundException`, `ValidationException`, `DomainException` to RFC 7807.
- `Infrastructure/Logging/CorrelationIdMiddleware.cs` — generates / propagates `X-Correlation-Id`.
- `Repositories/EventDbContext.cs` — empty DbContext shell.
- `Controllers/HealthController.cs` — `GET /api/v1/health` → `200 OK`, anonymous.
- `scripts/run.ps1`, `scripts/clean.ps1`.
- Test project skeleton `tests/EventService.Tests/`.

**Verification**
- `dotnet build` succeeds.
- `./scripts/run.ps1` starts the service on `0.0.0.0:5001`.
- `GET http://localhost:5001/api/v1/health` returns `200`.
- `GET http://localhost:5001/swagger` loads.
- `GET http://localhost:5001/api/v1/openapi.json` returns valid OpenAPI 3.1.
- A request without `Authorization` header to a (future) protected route returns `401` with Problem Details body.

---

## Phase 2 — Suppliers vertical slice (no BSS dependency)

**Goal:** Master supplier list — full CRUD + seed data.

**Deliverables**
- `Domain/Entities/Supplier.cs`, `Domain/ValueObjects/Money.cs` (placeholder for later).
- `Repositories/ISupplierRepository.cs` + `SupplierRepository.cs`.
- `Services/ISupplierService.cs` + `SupplierService.cs`.
- `Controllers/SuppliersController.cs` — `GET /suppliers`, `GET /suppliers/{id}`, `POST /suppliers`, `PUT /suppliers/{id}`, `DELETE /suppliers/{id}` (deactivate).
- `Migrations/0001_CreateSuppliers.cs` — generated by EF Core.
- `Infrastructure/Persistence/SeedRunner.cs` + `SupplierSeed.cs` — gated by `Persistence:SeedDataEnabled`; idempotent (skip if table non-empty).
- `Infrastructure/Validation/SupplierValidators.cs` — FluentValidation rules (per LLD §6).
- Unit tests: `Unit/Services/SupplierServiceTests.cs` (happy + edge).

**Verification**
- DB file created at configured path; migration applied.
- Seeded suppliers visible via `GET /suppliers`.
- CRUD round-trip via Swagger UI.
- All unit tests green.

---

## Phase 3 — Events + LineItems vertical slice (no BSS dependency)

**Goal:** Event header + line items + audit log persistence.

**Deliverables**
- `Domain/Entities/Event.cs` (with `Version` for optimistic concurrency), `Domain/Entities/LineItem.cs`, `Domain/Entities/AuditEvent.cs`.
- `Domain/Enums/EventStatus.cs`, `Domain/Enums/AuditAction.cs`.
- Repositories: `IEventRepository`, `ILineItemRepository`, `IAuditRepository` + impls.
- Services: `IEventService`, `ILineItemService`, `IAuditService` + impls. Audit emitted from Service layer (LLD §3.2).
- Controllers: `EventsController` (`POST /events`, `GET /events`, `GET /events/{id}`, `PUT /events/{id}`), `LineItemsController` (`POST /events/{id}/line-items`, `GET /events/{id}/line-items`, `DELETE /events/{id}/line-items/{itemId}`).
- `Migrations/0002_AddEventsLineItemsAudit.cs`.
- Validators (FluentValidation) per LLD §6 (title 3–200, category 2–80, currency=INR, deadline-in-future, etc.).
- Unit tests: state-transition guards (only Draft mutable), validation, audit emission.

**Verification**
- Create event → add line items → fetch event → audit table contains `EventCreated`, `LineItemAdded` rows with correlation IDs.
- Mutating a non-Draft event returns `409 Conflict` Problem Details.

---

## Phase 4 — Invitations vertical slice (no BSS dependency)

**Goal:** Invite suppliers to a Draft event.

**Deliverables**
- `Domain/Entities/EventSupplier.cs` (composite key).
- `IInvitationRepository` + impl, `IInvitationService` + impl, `InvitationsController` (`POST /events/{id}/invitations`, `GET /events/{id}/invitations`, `DELETE /events/{id}/invitations/{supplierId}`).
- `Migrations/0003_AddInvitations.cs`.
- Validators: event must be Draft; supplier must exist + be active; idempotent re-invite returns `409`.
- Unit tests.

**Verification**
- Invite a seeded supplier → list invitations → delete invitation → audit rows present.

---

## Phase 5 — Publish vertical slice (BSS gate deferred)

**Goal:** Transition `Draft → Published` with all locally-checkable gates.

**Deliverables**
- `POST /events/{id}/publish` on `EventsController`.
- Service-layer publish gates **implemented now**:
  - Event is in `Draft`.
  - At least one line item.
  - At least one invitation.
  - Response deadline is in the future.
- **Gate deferred until BSS available** (per LLD §13.2):
  - "≥1 scoring criterion exists for this event" — requires `GET /api/v1/events/{id}/criteria` on BSS.
  - Code carries a `// TODO(BSS): publish criteria-count gate` marker.
  - OpenAPI description on the publish endpoint explicitly notes this gate is currently not enforced.
- Audit `EventPublished` emitted.
- Unit tests for each gate; one test marked `[Fact(Skip = "Awaiting BSS criteria endpoint")]` for the deferred gate.

**Verification**
- Publish a fully-prepared event → status flips to `Published`; status visible via `GET /events/{id}`.
- Publish with missing line items / no invitees / past deadline → `409` Problem Details.

---

## Phase 6 — BSS-dependent surface (INTENTIONALLY OMITTED — incorporated later)

The following endpoints and behaviours are **not built in this iteration**. They are listed here so that, after BSS lands, they can be added in a clearly-scoped follow-up.

| Item                                                | LLD ref      | BSS dependency                                                          |
| --------------------------------------------------- | ------------ | ----------------------------------------------------------------------- |
| `POST /events/{id}/award`                           | LLD §5, §13  | Calls `GET /api/v1/events/{id}/bids/scored` on BSS for ranked list.    |
| Publish criteria-count gate                         | LLD §13.2    | Calls `GET /api/v1/events/{id}/criteria/count` on BSS.                 |
| `Bidding` / `Scored` advisory state transitions     | LLD §4.4     | Driven by BSS activity.                                                 |
| `IBidScoringClient` typed HttpClient + Polly pipeline | LLD §11    | Wired only when BSS endpoints land.                                     |

**Marker convention in code:** every deferred call site carries `// TODO(BSS-INTEGRATION): <what>` so a later grep finds them all.

---

## Phase 7 — Integration test sweep

**Goal:** End-to-end happy path verified in test code (no manual Postman dependency).

**Deliverables**
- `tests/EventService.Tests/Integration/HappyPathTests.cs` using `WebApplicationFactory<Program>` and a per-test SQLite file.
- Scenario: seed → create event → add 2 line items → invite 2 suppliers → publish → fetch event → assert status `Published` + audit rows.
- One auth test: missing token → `401`; valid token (signed with the same test secret) → `200`.

**Verification**
- `dotnet test` green from a clean checkout.

---

## Phase 8 — Documentation & release polish

**Deliverables**
- `README.md` — prerequisites (.NET 9 SDK, PowerShell 7), how to copy `appsettings.Example.json` → `appsettings.json`, run script usage, sample requests, link to OpenAPI.
- `CONTRIBUTING.md` — branch naming, commit style, run-tests checklist, code-style notes.
- `TROUBLESHOOTING.md` — DB locked, port 5001 in use, JWT signature mismatch with Auth Service, migration drift.
- `CHANGELOG.md` — entries per phase under `[0.1.0] – 2026-05-XX`.
- OpenAPI metadata polish (titles, descriptions, examples).

**Verification**
- A reviewer who has never seen the repo can clone, follow the README, and reach a working `GET /api/v1/health` in under 10 minutes.

---

## Critical files to be created/modified

- `services/event-service/EventService.csproj`
- `services/event-service/Program.cs`
- `services/event-service/appsettings.Development.json`, `appsettings.Example.json`
- `services/event-service/Infrastructure/{Auth,Errors,Logging,Persistence,Validation,Http}/*.cs`
- `services/event-service/Domain/Entities/*.cs`, `Domain/Enums/*.cs`
- `services/event-service/Repositories/EventDbContext.cs` + per-aggregate repo files
- `services/event-service/Services/*.cs`
- `services/event-service/Controllers/*.cs`
- `services/event-service/Migrations/*.cs` (EF Core generated)
- `services/event-service/tests/EventService.Tests/**/*.cs`
- `services/event-service/{README,CONTRIBUTING,TROUBLESHOOTING,CHANGELOG}.md`, `LICENSE`, `.gitignore`

## Open assumptions for Ramkumar to confirm at approval

1. **`appsettings.Development.json` is committed**, but `appsettings.json` / `appsettings.Staging.json` / `appsettings.Production.json` are gitignored. (Your directive said "appsettings.json is by default NOT version controlled" — confirming this scope.)
2. **MIT LICENSE** holder line will use `Copyright (c) 2026 Ramkumar` unless told otherwise.
3. **OpenAPI version**: .NET 9's built-in OpenAPI emits 3.1 by default; Swashbuckle is added only for the Swagger UI page.
4. **No Postman collection** in this build (since you chose unit-only per phase + one integration sweep).
5. **No Gateway integration** in this build; Event Service is reached directly on `:5001`.

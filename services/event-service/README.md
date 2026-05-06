# Event Service

Microservice that owns the **sourcing-event lifecycle** and the **organisation-wide supplier master list** for the RFx Sourcing system.

> **Note:** Endpoints that depend on the Bid Scoring Service (BSS) — Award and the BSS scoring-criteria publish gate — are intentionally omitted in this build. They are flagged with `// TODO(BSS-INTEGRATION)` in the code and documented in [docs/impl-event-service-plan.md](docs/impl-event-service-plan.md).

## Stack

| Layer           | Technology                                         |
| --------------- | -------------------------------------------------- |
| Runtime         | .NET 9 / ASP.NET Core Web API                      |
| Database        | SQLite (file-based) + EF Core 9, migrations on startup |
| Authentication  | JWT Bearer HS256, validated locally                |
| Logging         | Serilog, JSON console sink, correlation-ID enrichment |
| Validation      | FluentValidation (auto-validation via MVC filter)  |
| API docs        | OpenAPI 3.1 + Swagger UI (Swashbuckle)             |
| Tests           | xUnit + Moq (unit) · WebApplicationFactory (integration) · plain HttpClient (live-server) |

## Prerequisites

| Tool                   | Version  | Notes                                          |
| ---------------------- | -------- | ---------------------------------------------- |
| .NET SDK               | 9.0+     | `dotnet --version`                             |
| PowerShell             | 7.0+     | `pwsh --version`                               |
| Authentication Service | running  | Issues JWT tokens at `http://localhost:5003`   |

## First-time setup

The base `appsettings.json` is **gitignored** and must be created from the template:

```powershell
Copy-Item appsettings.Example.json appsettings.json
```

Open `appsettings.json` and set `Jwt:SigningKey` to the **same value** used by the Authentication Service (`JWT_SIGNING_KEY` in its `.env`). The key must be at least 32 characters.

## Run

```powershell
./scripts/run.ps1
```

The service starts on `http://0.0.0.0:5001`. On startup it:

1. Applies any pending EF Core migrations.
2. Seeds the supplier master list (idempotent — skipped if already seeded).
3. Prints a Spectre.Console banner summarising configuration and all available endpoints.

Stop with **Ctrl+C** — the service drains in-flight requests before exiting.

## API endpoints

All endpoints (except `/api/v1/health`) require `Authorization: Bearer <jwt>`.

### Suppliers

| Method | Path                        | Description                                      |
| ------ | --------------------------- | ------------------------------------------------ |
| GET    | `/api/v1/suppliers`         | Paginated supplier list (`?limit=20&offset=0&activeOnly=true`) |
| GET    | `/api/v1/suppliers/{id}`    | Single supplier by ID                            |

### Events

| Method | Path                             | Description                                  |
| ------ | -------------------------------- | -------------------------------------------- |
| POST   | `/api/v1/events`                 | Create a Draft event                         |
| GET    | `/api/v1/events`                 | Paginated event list (`?limit=20&offset=0`)  |
| GET    | `/api/v1/events/{id}`            | Single event by ID                           |
| PUT    | `/api/v1/events/{id}`            | Update a Draft event (409 if not Draft)      |
| POST   | `/api/v1/events/{id}/publish`    | Publish a Draft event (local gates enforced) |

### Line items

| Method | Path                                          | Description                          |
| ------ | --------------------------------------------- | ------------------------------------ |
| POST   | `/api/v1/events/{id}/line-items`              | Add a line item to a Draft event     |
| GET    | `/api/v1/events/{id}/line-items`              | List all line items for an event     |
| DELETE | `/api/v1/events/{id}/line-items/{itemId}`     | Remove a line item from a Draft event |

### Invitations

| Method | Path                                               | Description                                |
| ------ | -------------------------------------------------- | ------------------------------------------ |
| POST   | `/api/v1/events/{id}/invitations`                  | Invite an active supplier to a Draft event |
| GET    | `/api/v1/events/{id}/invitations`                  | List all invitations for an event          |
| DELETE | `/api/v1/events/{id}/invitations/{supplierId}`     | Revoke an invitation (Draft only)          |

### Infrastructure

| Method | Path                    | Auth | Description          |
| ------ | ----------------------- | ---- | -------------------- |
| GET    | `/api/v1/health`        | None | Liveness probe       |
| GET    | `/api/v1/openapi.json`  | None | OpenAPI 3.1 document |
| GET    | `/swagger`              | None | Swagger UI           |

## Sample requests

All examples assume the service is running and `$TOKEN` holds a valid Buyer JWT (see [Obtain a token](#obtain-a-token) below).

### Obtain a token

```powershell
$resp = Invoke-RestMethod -Method Post `
    -Uri http://localhost:5003/api/v1/auth/login `
    -ContentType "application/json" `
    -Body '{"username":"buyer1","password":"Password1!"}'

$TOKEN = $resp.token
```

### List seeded suppliers

```powershell
Invoke-RestMethod -Uri http://localhost:5001/api/v1/suppliers `
    -Headers @{ Authorization = "Bearer $TOKEN" }
```

### Create a sourcing event

```powershell
$event = Invoke-RestMethod -Method Post `
    -Uri http://localhost:5001/api/v1/events `
    -Headers @{ Authorization = "Bearer $TOKEN" } `
    -ContentType "application/json" `
    -Body '{
        "title": "Cloud Infrastructure RFP 2026",
        "description": "Annual refresh of compute and storage.",
        "category": "IT",
        "currency": "INR",
        "responseDeadlineUtc": "2026-08-01T00:00:00Z"
    }'

$eventId = $event.id
```

### Add a line item

```powershell
Invoke-RestMethod -Method Post `
    -Uri "http://localhost:5001/api/v1/events/$eventId/line-items" `
    -Headers @{ Authorization = "Bearer $TOKEN" } `
    -ContentType "application/json" `
    -Body '{
        "description": "Rack-mount compute nodes",
        "quantity": 10,
        "unitPrice": 250000
    }'
```

### Invite a supplier

```powershell
# Pick any supplier ID from the list response
$supplierId = (Invoke-RestMethod `
    -Uri http://localhost:5001/api/v1/suppliers `
    -Headers @{ Authorization = "Bearer $TOKEN" }).items[0].id

Invoke-RestMethod -Method Post `
    -Uri "http://localhost:5001/api/v1/events/$eventId/invitations" `
    -Headers @{ Authorization = "Bearer $TOKEN" } `
    -ContentType "application/json" `
    -Body "{`"supplierId`": `"$supplierId`"}"
```

### Publish the event

```powershell
Invoke-RestMethod -Method Post `
    -Uri "http://localhost:5001/api/v1/events/$eventId/publish" `
    -Headers @{ Authorization = "Bearer $TOKEN" }
```

## Running tests

### Unit + in-process integration tests

These run without any external dependency (the in-process tests spin up an isolated SQLite instance per class):

```powershell
dotnet test
```

Expected: **47 passed, 1 skipped** (BSS criteria-count gate placeholder) + **11 skipped** (live-server tests when server is not running).

### Live-server tests

These exercise the real Kestrel process over the network. Start the service first:

```powershell
# Terminal 1
./scripts/run.ps1

# Terminal 2
dotnet test --filter "Category=LiveServer"
```

The `EVENT_SERVICE_BASE_URL` environment variable overrides the default `http://localhost:5001`:

```powershell
$env:EVENT_SERVICE_BASE_URL = "http://staging-host:5001"
dotnet test --filter "Category=LiveServer"
```

If the server is not reachable the tests report **Skipped**, not Failed.

## Reset local database

```powershell
./scripts/clean.ps1
```

Deletes `data/event_service.db`. The next `run.ps1` recreates the schema and re-seeds.

## Project layout

```
EventService.csproj
Program.cs
appsettings.Development.json   (committed — developer defaults)
appsettings.Example.json       (committed — template; copy to appsettings.json)
appsettings.json               (gitignored — you create it)
Controllers/      HTTP entry points
Services/         Business logic + interfaces
Repositories/     EF Core DbContext + repos
Domain/           Entities, enums, value objects
Infrastructure/   Auth, Errors, Logging, Persistence
Migrations/       EF Core generated
scripts/          run.ps1, clean.ps1
tests/
  EventService.Tests/
    Unit/           xUnit + Moq unit tests
    Integration/    WebApplicationFactory-based end-to-end tests
    LiveServer/     Black-box tests against the running Kestrel process
docs/             BRD, HLD, LLD, implementation plan
```

## Documentation

- [BRD](docs/BRD.md) — business requirements
- [HLD](docs/HLD.md) — system architecture
- [LLD](docs/LLD-event-service.md) — service-level design
- [Implementation plan](docs/impl-event-service-plan.md) — phased build plan
- [CONTRIBUTING.md](CONTRIBUTING.md) — how to contribute
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — common issues
- [CHANGELOG.md](CHANGELOG.md) — version history

## License

[MIT](LICENSE)

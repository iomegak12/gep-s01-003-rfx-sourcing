# Troubleshooting

Common issues encountered while running or testing the Event Service locally and how to resolve them.

---

## Service fails to start

### `appsettings.json` not found

**Symptom:** Process exits immediately with a configuration error.

**Cause:** The base `appsettings.json` is gitignored and must be created by the developer.

**Fix:**

```powershell
Copy-Item appsettings.Example.json appsettings.json
```

Then set `Jwt:SigningKey` to the value matching the Authentication Service's `JWT_SIGNING_KEY`.

---

### Port 5001 already in use

**Symptom:** `Failed to bind to address http://0.0.0.0:5001: address already in use`.

**Fix (Windows):**

```powershell
Get-NetTCPConnection -LocalPort 5001 | Select-Object OwningProcess
Stop-Process -Id <pid>
```

Or change `Kestrel:Endpoints:Http:Url` in `appsettings.json`.

---

## Authentication issues

### Every request returns `401 Unauthorized`

**Cause #1:** Missing `Authorization: Bearer <jwt>` header.

**Cause #2:** Token signature does not validate.

**Fix:** The `Jwt:SigningKey` in this service's `appsettings.json` must **exactly match** the Authentication Service's `JWT_SIGNING_KEY`. Both services must also agree on:

- `Jwt:Issuer` → `rfx-auth-service`
- `Jwt:Audience` → `rfx-sourcing-system`

Verify the Authentication Service is running:

```powershell
Invoke-RestMethod http://localhost:5003/api/v1/health
```

Obtain a token:

```powershell
$resp = Invoke-RestMethod -Method Post `
    -Uri http://localhost:5003/api/v1/auth/login `
    -ContentType "application/json" `
    -Body '{"username":"buyer1","password":"Password1!"}'

$TOKEN = $resp.token
```

---

### `403 Forbidden` on a protected endpoint

**Cause:** Token is valid but does not carry the `Buyer` role.

**Fix:** Authenticate as a user whose `roles` claim includes `Buyer`.

---

## Publish returns `409 Conflict`

The publish gate enforces four local conditions — the response body (`detail` field) identifies which one failed:

| Detail message                   | Fix                                                   |
| -------------------------------- | ----------------------------------------------------- |
| Event is not in Draft status     | Only Draft events can be published                    |
| Response deadline is in the past | Update the event (`PUT /api/v1/events/{id}`) with a future deadline |
| No line items                    | Add at least one line item before publishing          |
| No suppliers invited             | Invite at least one active supplier before publishing |

---

## Database issues

### `SqliteException: database is locked`

**Cause:** Another process (or DB Browser for SQLite) holds the file open.

**Fix:** Close other clients. If it persists:

```powershell
./scripts/clean.ps1
./scripts/run.ps1
```

---

### `PendingModelChangesWarning` at startup

**Cause:** The EF Core model has changed but a migration has not been generated.

**Fix:**

```powershell
dotnet ef migrations add <DescriptiveName>
```

---

### Migrations table out of sync

**Symptom:** Schema looks wrong; `__EFMigrationsHistory` shows fewer rows than expected.

**Fix (local only):** Delete the database and let startup reapply everything:

```powershell
./scripts/clean.ps1
./scripts/run.ps1
```

---

## Test issues

### `dotnet test` failures in `Integration/` tests

**Symptom:** `CreatedAtAction` or routing-related `InvalidOperationException` in tests.

**Background:** The `TestServer` environment resolves route constraints differently from a real Kestrel instance. All controllers use explicit `Created(uri, value)` calls (not `CreatedAtAction`) to avoid this.

**Fix:** If you add a new controller action that returns `201 Created`, use `Created($"/api/v1/...", result)` with an explicit path string rather than `CreatedAtAction`.

---

### Enums serialise as integers instead of strings in tests

**Symptom:** Assertions like `Assert.Equal("Draft", json.GetProperty("status").GetString())` fail because the value is `0`.

**Fix:** Ensure `AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` is present in `Program.cs`. This is already in place — if you see this in a new context, check that the test factory is not overriding the serialiser options.

---

### Live-server tests show as `Skipped` instead of running

**Cause:** The Event Service is not running at `http://localhost:5001` (or the value of `EVENT_SERVICE_BASE_URL`).

**Fix:** Start the service first, then run the live-server filter:

```powershell
# Terminal 1
./scripts/run.ps1

# Terminal 2
dotnet test --filter "Category=LiveServer"
```

Override the target URL if testing against a non-default host:

```powershell
$env:EVENT_SERVICE_BASE_URL = "http://staging-host:5001"
dotnet test --filter "Category=LiveServer"
```

---

### Live-server tests fail with `401` even though the service is up

**Cause:** The JWT signing key used by the test fixture (`EVENT_SERVICE_JWT_KEY` env var, default is the dev key in `appsettings.Development.json`) does not match the key the running service validates against.

**Fix:** Set `EVENT_SERVICE_JWT_KEY` to the same value as `Jwt:SigningKey` in the service's `appsettings.json`:

```powershell
$env:EVENT_SERVICE_JWT_KEY = "your-actual-signing-key"
dotnet test --filter "Category=LiveServer"
```

---

## Logging

### No log output in the console

**Cause:** Serilog configuration not applied.

**Fix:** Confirm the `Serilog` section exists in `appsettings.json` and that `Program.cs` calls `UseSerilog()`.

---

### Cannot trace a request across services

Every inbound request is tagged with `X-Correlation-Id` (auto-generated if missing). The same ID appears in every Serilog log entry for that request. Pass `X-Correlation-Id: <your-id>` in the request header to propagate a known ID from an upstream caller.

---

## Build issues

### Missing XML doc warnings

The project enables `GenerateDocumentationFile` and suppresses CS1591 (missing XML on public members) for domain entities and enums, which are intentionally undocumented. Other CS1591 warnings on Controllers, Services, or Repositories should be addressed by adding an XML `<summary>` tag.

---

## Still stuck?

Open an issue with:

- Exact command run.
- Full console output (with correlation IDs if available).
- Expected vs actual behaviour.
- `dotnet --info` output.
- Contents of `appsettings.json` **with secrets redacted**.

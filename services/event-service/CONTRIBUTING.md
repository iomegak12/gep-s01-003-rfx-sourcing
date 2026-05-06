# Contributing

Thank you for considering a contribution to the Event Service.

## Branching

- `main` — protected; merged only via pull request after review.
- Feature branches — `feature/<short-description>` (e.g. `feature/suppliers-crud`).
- Fix branches — `fix/<short-description>`.

## Commit style

Conventional commits, lowercase:

```
feat(suppliers): add supplier CRUD endpoints
fix(events): reject mutation of non-Draft events
docs(readme): clarify JWT signing key setup
test(invitations): cover idempotent re-invite
chore(deps): bump EF Core to 9.0.x
```

## Pull request checklist

Before requesting review:

- [ ] `dotnet build` succeeds with no warnings.
- [ ] `dotnet test` is green (unit + integration; live-server tests may be skipped if server is not running — that is expected).
- [ ] New behaviour is covered by unit tests; significant flows have integration test coverage.
- [ ] Public Controllers, Services, Repositories, and Infrastructure helpers carry XML doc comments.
- [ ] OpenAPI surface (`/swagger`) reflects any endpoint additions or changes.
- [ ] No secrets, no `appsettings.json`, no `.db` files committed.
- [ ] `CHANGELOG.md` updated under `## [Unreleased]`.

## Architecture rules

Follow the four-layer separation:

```
Controller → Service → Repository → Domain
```

- **Controllers** — bind, validate via FluentValidation auto-validation, delegate to a service, map the result to an HTTP response. No business logic.
- **Services** — enforce all business rules, emit audit entries via `IAuditService`. Never access `HttpContext`.
- **Repositories** — data access only. Return domain entities; never leak EF Core tracked aggregates.
- **Domain** — entities, enums, value objects. No infrastructure dependencies.

Every concrete class with behaviour has an `I<Name>` interface and is registered in DI. Prefer constructor injection.

Throw application-defined exceptions (`NotFoundException`, `ValidationException`, `DomainException`) — the global `ProblemDetailsExceptionHandler` maps them to RFC 7807 responses.

## Test categories

| Folder                    | Tool                         | Runs without server? |
| ------------------------- | ---------------------------- | -------------------- |
| `Unit/`                   | xUnit + Moq                  | Yes                  |
| `Integration/`            | xUnit + WebApplicationFactory | Yes (in-process SQLite) |
| `LiveServer/`             | xUnit + plain HttpClient     | No — needs `run.ps1` |

Run only unit + integration tests:

```powershell
dotnet test --filter "Category!=LiveServer"
```

Run only live-server tests (service must be running):

```powershell
dotnet test --filter "Category=LiveServer"
```

## Adding a new endpoint

1. Add the domain entity / enum change to `Domain/`.
2. Add or extend the repository interface + implementation in `Repositories/`.
3. Add or extend the service interface + implementation in `Services/`.
4. Add the controller action (or new controller) in `Controllers/`.
5. Add a FluentValidation validator in `Services/<Feature>/` if the endpoint accepts a request body.
6. Add unit tests in `Unit/`.
7. Add or extend the integration test scenario in `Integration/HappyPathTests.cs` if it affects the happy path.
8. Add a `[SkippableFact]` in `LiveServer/LiveServerTests.cs` for the new HTTP surface.
9. Generate and review the EF Core migration if the domain model changed:
   ```powershell
   dotnet ef migrations add <DescriptiveName>
   ```
10. Update `CHANGELOG.md`.

## Deferred BSS endpoints

Anything that depends on the Bid Scoring Service (Award endpoint, BSS criteria-count publish gate) is **intentionally omitted**. Deferred call sites are marked with:

```csharp
// TODO(BSS-INTEGRATION): <what needs to happen>
```

Do not implement these until BSS is available and coordination with that team has taken place.

## Reporting issues

Open an issue with:

- Exact command run.
- Full console output (with correlation IDs where available).
- Expected vs actual behaviour.
- `dotnet --info` output.
- Contents of `appsettings.json` **with secrets redacted**.

# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

## [0.1.0] — 2026-05-05

### Added

**Core service (Phase 1)**
- Node.js 22 + Express 5 ESM scaffold with `"type": "module"`
- Zod-validated environment configuration — fail-fast on startup if required vars are missing
- Colorful startup banner (chalk + boxen) showing network URL, environment, endpoints, docs link, and rate-limit status
- Pino structured JSON logging to stdout with ISO timestamps and `service` base field

**Observability & error plumbing (Phase 2)**
- `X-Correlation-Id` header — generated (UUIDv4) per request when absent, echoed in response, attached to every log line
- pino-http request/response logger with correlation ID propagation
- RFC 7807 Problem Details error shape (`application/problem+json`) for all error responses, per ADR 0005
- `AppError` base class and five typed subclasses: `ValidationError`, `InvalidCredentialsError`, `InvalidAccessTokenError`, `InvalidRefreshTokenError`, `InternalError`
- Global error-handler middleware — last middleware in the chain, maps any thrown error to Problem Details
- `GET /api/v1/health` liveness probe returning `{ status, service, timestamp }`

**Persistence & seeding (Phase 3)**
- SQLite via `better-sqlite3` (WAL mode, foreign keys on); database file created at `DATABASE_PATH` on first boot
- Idempotent `CREATE TABLE IF NOT EXISTS users` migration run on every startup
- Seed-on-startup: three pre-hashed users inserted when the table is empty and `SEED_DATA_ENABLED=true`
  - `buyer.user` / `Buyer@123` / roles `["Buyer"]`
  - `supplier.user` / `Supplier@123` / roles `["Supplier"]`
  - `admin.user` / `Admin@123` / roles `["Admin", "All"]`
- Stable hardcoded UUIDs so JWT `sub` claims are predictable across restarts
- Argon2id password hashing (RFC 9106 / OWASP): 64 MiB memory, 2 iterations, parallelism 1, hash length 32 bytes; full PHC string stored; `needsRehash` used for forward-compatible param upgrades

**Token & password services (Phase 4)**
- `src/services/password.service.js` — `hash`, `verify`, `needsRehash` wrapping the `argon2` package
- `src/services/token.service.js` — HS256 JWT signing and verification:
  - `signAccessToken(user)` — claims: `sub`, `iat`, `exp`, `iss`, `aud`, `tokenType: "access"`, `roles`
  - `signRefreshToken(user)` — same but `tokenType: "refresh"`, no `roles`
  - `verifyAccessToken(token)` / `verifyRefreshToken(token)` — enforce `iss`, `aud`, `tokenType`, and expiry; throw typed errors on mismatch
- `src/domain/roles.js` — frozen `ROLES` constants: `Buyer`, `Supplier`, `Admin`, `All`
- Unit tests (15): password round-trip, PHC format, wrong password, needsRehash; token sign/verify, tampered sig, expired, wrong iss/aud, cross-type rejection

**Auth endpoints (Phase 5)**
- `POST /api/v1/auth/login` — Zod-validated body; Argon2id verify; opportunistic rehash on param upgrade; returns `{ accessToken, refreshToken, tokenType, accessTokenExpiresInSeconds, refreshTokenExpiresInSeconds }`
- `POST /api/v1/auth/refresh` — verifies refresh token; re-resolves roles from DB for freshness; returns new access token
- `GET /api/v1/auth/me` — Bearer guard middleware; returns `{ userId, username, roles, tokenIssuedAt, tokenExpiresAt }` derived from the access token and user record
- Integration tests (21): happy path × 3 users for each endpoint; 400/401 error cases for all three endpoints

**Rate limiting (Phase 6)**
- `express-rate-limit` applied to `/api/v1/auth/login` and `/api/v1/auth/refresh` only
- Disabled by default (`RATE_LIMIT_ENABLED=false`); toggled at startup via env
- 429 responses use Problem Details shape (`application/problem+json`) with `RateLimit-*` standard headers
- `createApp(opts)` accepts `opts.rateLimit` override so integration tests can inject a low threshold without env mutation
- Integration tests (5): threshold enforcement, 429 shape, standard headers, refresh limited, health unaffected

**API documentation (Phase 7)**
- OpenAPI 3.1 spec at `src/openapi/openapi.yaml` covering all four endpoints with full request/response schemas, `ProblemDetails` reusable component, and `bearerAuth` security scheme
- `GET /api/openapi.json` — serves the spec as JSON for tooling
- `GET /api/docs/` — Swagger UI served by `swagger-ui-express`
- Integration tests (6): JSON 200, version matches `package.json`, four paths present, `ProblemDetails` component defined, `bearerAuth` scheme defined, Swagger UI HTML served

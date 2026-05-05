# Auth Service — Implementation Plan

## RFx Sourcing Event Management

---

### Document Control

| Field             | Value                                                                |
| ----------------- | -------------------------------------------------------------------- |
| Document Title    | Auth Service Implementation Plan — RFx Sourcing Event Management     |
| Version           | 0.1 (Draft)                                                          |
| Status            | For Implementation                                                   |
| Author            | Ramkumar (Solution Architect)                                        |
| Date              | 05 May 2026                                                          |
| Related Artefacts | Auth_Service_Specification.md v0.1, HLD.md v0.1, ADR 0005, ADR 0006  |
| Stack             | Node.js 22 + Express 5 (ESM), SQLite (better-sqlite3)                |
| Companion ADR     | (forthcoming) — Password Hashing with Argon2id                       |

---

## 1. Context

The RFx Sourcing system has a contract-level [Auth Service Specification](Auth_Service_Specification.md) that originally framed the Auth Service as pre-built. We are now bringing it **into scope** and implementing it inside the mono-repo at [services/authentication-service/](..). Today the folder contains only the spec doc — no code.

The implementation must satisfy the JWT/HS256 contract that the API Gateway and downstream services depend on:
- Issuer `rfx-auth-service`, audience `rfx-sourcing-system`, `tokenType` claim discriminating access vs refresh.
- RFC 7807 Problem Details for all error responses (per ADR 0005).
- Four endpoints under `/api/v1/...` (per ADR 0004).

Stack is **Node.js 22 + Express 5**, persistence is **SQLite** with seed-on-startup, and the build is delivered **phase by phase**.

---

## 2. Resolved Decisions

| #  | Item                  | Decision                                                                                  |
| -- | --------------------- | ----------------------------------------------------------------------------------------- |
| D1 | Bind port             | **5003** (per HLD §6.2 / spec §2). Host `0.0.0.0`. Port `5000` is reserved for the Gateway.|
| D2 | Entry file            | `src/server.js` (ESM, Node 22 + `"type": "module"`).                                      |
| D3 | Roles                 | Four roles: `Buyer`, `Supplier`, `Admin`, `All`. `All` is a super-role wildcard.          |
| D4 | User store            | SQLite + idempotent seed on startup (3 users).                                            |
| D5 | Out-of-scope features | Excluded per spec §13 (no registration, MFA, SSO, lockout, refresh rotation, JWKS, etc.). |
| D6 | Password hashing      | **Argon2id** per architecture review. Memory-hard, OWASP/RFC 9106 default.                |
| D7 | Hash storage          | Single `password_hash` TEXT column holding the **full PHC string**.                       |
| D8 | Forward compatibility | `argon2.needsRehash()` invoked on successful login; transparent rehash if params drift.   |

---

## 3. Stack Defaults

| Concern                | Choice                                                                                           |
| ---------------------- | ------------------------------------------------------------------------------------------------ |
| Web framework          | Express 5                                                                                        |
| Module system          | ESM (`"type": "module"`), plain JavaScript                                                       |
| Logger                 | `pino` + `pino-http` (structured JSON to stdout, per HLD §11.1)                                  |
| Startup banner         | `chalk` + `boxen` (colorful banner, console only — does not pollute structured logs)             |
| Config validation      | `zod` (env vars + request bodies)                                                                |
| JWT                    | `jsonwebtoken` (HS256)                                                                           |
| Password hashing       | `argon2` (npm) — Argon2id; Windows fallback `@node-rs/argon2` documented in TROUBLESHOOTING       |
| SQLite driver          | `better-sqlite3` (synchronous, zero-install)                                                     |
| Rate limiting          | `express-rate-limit` (disabled by default; configurable via env)                                 |
| API documentation      | Hand-written OpenAPI 3.1 YAML + `swagger-ui-express` at `/api/docs`                              |
| Unit tests             | `vitest`                                                                                         |
| Integration tests      | `supertest`                                                                                      |
| Coverage               | `@vitest/coverage-v8` (target ≥80% statements, informational not gating)                          |
| Security               | `helmet`, `cors`                                                                                 |

---

## 4. Argon2id Configuration

Per the architecture team's recommendation (RFC 9106, OWASP):

| Parameter         | Value                | Env Variable                | Notes                                              |
| ----------------- | -------------------- | --------------------------- | -------------------------------------------------- |
| Algorithm         | `argon2id`           | (constant)                  | Hybrid resistant to side-channel + GPU/ASIC.       |
| Memory cost       | 64 MiB (65536 KiB)   | `ARGON2_MEMORY_COST_KIB`    | 19 MiB (19456 KiB) is the floor.                   |
| Time cost         | 2 iterations         | `ARGON2_TIME_COST`          |                                                    |
| Parallelism       | 1                    | `ARGON2_PARALLELISM`        |                                                    |
| Salt              | 16 bytes random      | (library default)           | Generated per password by the library.             |
| Hash length       | 32 bytes             | `ARGON2_HASH_LENGTH`        |                                                    |
| Storage format    | PHC string           | (constant)                  | `$argon2id$v=19$m=65536,t=2,p=1$<salt>$<hash>`     |

**Rehash hook**: `auth.service.login()` calls `argon2.needsRehash(stored, currentOptions)` after a successful verify. If true, the password is re-hashed with current params and persisted in the same transaction. This lets us ratchet work factor up over time without forcing a global password reset.

**FIPS-140 fallback**: if a deployment environment blocks Argon2id (FIPS compliance), the architecture team sanctioned `bcrypt` as an acceptable fallback. Out of scope for this build; flag and reassess if encountered.

---

## 5. Folder Layout

```
services/authentication-service/
├── .env.example                # version-controlled template
├── .gitignore                  # ignores .env, data/*.db, node_modules, coverage
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
├── TROUBLESHOOTING.md
├── package.json                # type: module, scripts: start, dev, test, test:int
├── vitest.config.js
├── docs/
│   ├── Auth_Service_Specification.md   (existing)
│   └── auth-service-impl-plan.md       (this document)
├── data/                       # gitignored — SQLite file lives here at runtime
└── src/
    ├── server.js               # entry point — boots app, listens on 0.0.0.0:5003
    ├── app.js                  # express app factory (testable, no listen)
    ├── config/
    │   ├── env.js              # zod-validated env loader
    │   ├── constants.js        # APP_NAME, APP_DESCRIPTION
    │   └── banner.js           # colorful startup banner (chalk + boxen)
    ├── infrastructure/
    │   ├── logger.js           # pino setup (JSON to stdout)
    │   ├── db.js               # better-sqlite3 instance
    │   └── migrations.js       # CREATE TABLE users + idempotent seed
    ├── domain/
    │   ├── roles.js            # Buyer | Supplier | Admin | All constants
    │   └── user.js             # user shape (jsdoc typedef)
    ├── repositories/
    │   └── user.repository.js  # findByUsername, findById, updatePasswordHash
    ├── services/
    │   ├── password.service.js # argon2.hash, argon2.verify, argon2.needsRehash
    │   ├── token.service.js    # signAccessToken, signRefreshToken, verify
    │   └── auth.service.js     # login, refresh, me orchestration
    ├── validation/
    │   └── auth.schemas.js     # zod schemas for login/refresh bodies
    ├── errors/
    │   ├── app-error.js        # base class
    │   ├── domain-errors.js    # ValidationError, InvalidCredentials, InvalidToken
    │   └── problem-details.js  # RFC 7807 mapper
    ├── middleware/
    │   ├── correlation-id.js   # X-Correlation-Id generation/propagation
    │   ├── request-logger.js   # pino-http
    │   ├── rate-limit.js       # express-rate-limit (toggle by env)
    │   ├── auth-required.js    # bearer guard for /auth/me
    │   └── error-handler.js    # global RFC 7807 emitter
    ├── controllers/
    │   ├── auth.controller.js
    │   └── health.controller.js
    ├── routes/
    │   ├── index.js            # mounts /api/v1
    │   ├── auth.routes.js
    │   └── health.routes.js
    ├── seed/
    │   └── users.seed.js       # 3 users with Argon2id PHC hashes
    └── openapi/
        └── openapi.yaml        # OpenAPI 3.1 spec
└── tests/
    ├── unit/
    │   ├── token.service.test.js
    │   ├── password.service.test.js
    │   └── auth.service.test.js
    └── integration/
        ├── health.test.js
        ├── auth.login.test.js
        ├── auth.refresh.test.js
        └── auth.me.test.js
```

---

## 6. `.env.example` (committed)

```dotenv
# Application
APP_NAME=auth-service
APP_DESCRIPTION="RFx Sourcing — Authentication Service (issues HS256 JWTs)"
NODE_ENV=development
HOST=0.0.0.0
PORT=5003
LOG_LEVEL=info

# JWT (must match Gateway's Jwt:* config)
JWT_SIGNING_KEY=replace-me-with-32-plus-char-random-string
JWT_ISSUER=rfx-auth-service
JWT_AUDIENCE=rfx-sourcing-system
JWT_ACCESS_TOKEN_LIFETIME_SECONDS=900
JWT_REFRESH_TOKEN_LIFETIME_SECONDS=86400

# Persistence
DATABASE_PATH=./data/auth.db
SEED_DATA_ENABLED=true

# Password hashing — Argon2id (RFC 9106 / OWASP defaults)
ARGON2_MEMORY_COST_KIB=65536
ARGON2_TIME_COST=2
ARGON2_PARALLELISM=1
ARGON2_HASH_LENGTH=32

# Rate limiting (disabled by default per requirement)
RATE_LIMIT_ENABLED=false
RATE_LIMIT_WINDOW_MS=60000
RATE_LIMIT_MAX=100
```

---

## 7. Database Schema

```sql
CREATE TABLE IF NOT EXISTS users (
    id            TEXT PRIMARY KEY,           -- UUID v4 (stable across boots for seeded users)
    username      TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,              -- Argon2id PHC string (full)
    roles         TEXT NOT NULL,              -- JSON-encoded string array, e.g. '["Admin","All"]'
    created_at    TEXT NOT NULL               -- ISO 8601
);
```

### Seeded Users

| Username        | Password         | Roles                  | Stable UUID (sub claim)                 |
| --------------- | ---------------- | ---------------------- | --------------------------------------- |
| `buyer.user`    | `Buyer@123`      | `["Buyer"]`            | `8f3b2c1a-1d4e-4a5b-9c7d-2e6f8a0b1c2d`  |
| `supplier.user` | `Supplier@123`   | `["Supplier"]`         | `7e2a1b09-0c3d-3a4b-8b6c-1d5e7a9b0b1e`  |
| `admin.user`    | `Admin@123`      | `["Admin", "All"]`     | `6d190a08-fb2c-2a3b-7a5b-0c4d6987a0d0`  |

Seed runs only when `SEED_DATA_ENABLED=true` **and** the `users` table is empty (idempotent).

---

## 8. Phase-by-Phase Plan

### Phase 1 — Project Scaffold
**Goal:** Empty Express project that boots and prints the colorful banner.

- Create `package.json` (Node 22, `"type": "module"`, scripts: `start`, `dev` via `--watch`, `test`, `test:int`, `lint`).
- Install runtime deps: `express`, `pino`, `pino-http`, `chalk`, `boxen`, `dotenv`, `zod`, `argon2`, `jsonwebtoken`, `better-sqlite3`, `express-rate-limit`, `swagger-ui-express`, `js-yaml`, `uuid`, `cors`, `helmet`.
- Install dev deps: `vitest`, `@vitest/coverage-v8`, `supertest`.
- Create `.gitignore` (`.env`, `data/`, `node_modules`, `coverage`, `dist`, OS junk).
- Create `.env.example` (per §6).
- Skeleton `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md` (`0.1.0 — initial scaffold`), `TROUBLESHOOTING.md`.
- `src/config/env.js` — load `.env`, validate via zod, export typed config object.
- `src/config/constants.js` — `APP_NAME`, `APP_DESCRIPTION` from env.
- `src/config/banner.js` — colorful startup banner: app name, description, version, host, port, environment, log level, available endpoints (placeholder list), docs URL.
- `src/server.js` — boots `app`, listens on `HOST:PORT`, prints banner via `console.log` after listen callback fires.
- `src/app.js` — minimal Express factory (helmet, cors, json body, request-logger).

**Verify:** `npm start` shows banner; server bound to `0.0.0.0:5003`; `curl http://localhost:5003/` returns 404 in JSON.

---

### Phase 2 — Health Endpoint, Correlation IDs, Error Plumbing
**Goal:** Observable; errors emit RFC 7807.

- `src/middleware/correlation-id.js` — read `X-Correlation-Id`, generate UUIDv4 if absent, attach to `req.correlationId`, echo in response header.
- `src/infrastructure/logger.js` — pino with `base: { service: APP_NAME }`; child logger keyed by `correlationId` per request.
- `src/middleware/request-logger.js` — pino-http with custom serializers; logs `method`, `url`, `status`, `durationMs`, `correlationId`.
- `src/errors/app-error.js`, `src/errors/domain-errors.js` — base + `ValidationError`, `InvalidCredentialsError`, `InvalidAccessTokenError`, `InvalidRefreshTokenError`, `InternalError`.
- `src/errors/problem-details.js` — maps an `AppError` → `{ type, title, status, detail, instance, correlationId }` with `Content-Type: application/problem+json` (per ADR 0005, spec §8).
- `src/middleware/error-handler.js` — terminal middleware that catches any thrown error, logs it, returns Problem Details.
- `src/controllers/health.controller.js` — returns `{ status: "UP", service, timestamp: ISO8601 }` (spec §5.4).
- `src/routes/health.routes.js` + `src/routes/index.js` — mount under `/api/v1`.

**Verify:** `GET /api/v1/health` returns 200 with body matching spec; `GET /api/v1/missing` returns 404 in Problem Details shape.

---

### Phase 3 — Persistence & Seed
**Goal:** SQLite with three Argon2id-hashed users available at boot.

- `src/infrastructure/db.js` — open `DATABASE_PATH` via `better-sqlite3`; ensure `data/` exists.
- `src/infrastructure/migrations.js` — `CREATE TABLE IF NOT EXISTS users (...)` per §7.
- `src/seed/users.seed.js` — three records when table empty AND `SEED_DATA_ENABLED=true`; passwords hashed using `password.service.hash()` so seed picks up current Argon2id config from env.
- UUIDs are stable (hardcoded constants) so tests can assert on `sub`.
- `src/repositories/user.repository.js` — `findByUsername(username)`, `findById(id)`, `updatePasswordHash(id, newHash)` (last is for the rehash-on-login path).
- Wire migrations + seed into `server.js` boot sequence (before `app.listen`).
- Banner now displays "Database: connected (N users)".

**Verify:** delete `data/auth.db`, run server, see seed log line with 3 users; second boot logs "seed skipped — table not empty".

---

### Phase 4 — Token & Password Services (Unit-Tested)
**Goal:** Pure functions ready for the auth flow.

- `src/services/password.service.js`:
  - `hash(plain)` → returns Argon2id PHC string using `{ type: argon2.argon2id, memoryCost, timeCost, parallelism, hashLength }` from config.
  - `verify(phc, plain)` → boolean.
  - `needsRehash(phc)` → boolean using `argon2.needsRehash(phc, currentOptions)`.
- `src/services/token.service.js`:
  - `signAccessToken(user)` → claims `{ sub, iat, exp, iss, aud, tokenType: 'access', roles }` per spec §6.2.
  - `signRefreshToken(user)` → claims same but `tokenType: 'refresh'` and **no `roles`** per spec §6.3.
  - `verifyAccessToken(jwt)`, `verifyRefreshToken(jwt)` → enforce `iss`, `aud`, `tokenType`, expiry.
- `src/domain/roles.js` — `export const ROLES = { BUYER, SUPPLIER, ADMIN, ALL }`.
- `tests/unit/password.service.test.js` — hash + verify round-trip; wrong password returns false; output is a PHC string starting with `$argon2id$`; `needsRehash` returns false for a freshly hashed value at current config; `needsRehash` returns true when a hash made with reduced memory cost is checked against current config.
- `tests/unit/token.service.test.js` — sign/verify round-trip; tampered signature rejected; expired token rejected; wrong `iss`/`aud` rejected; access-token-as-refresh and refresh-token-as-access both rejected.

---

### Phase 5 — Auth Endpoints
**Goal:** All four endpoints live and integration-tested.

- `src/validation/auth.schemas.js` — zod schemas for login (`username`, `password` non-empty) and refresh (`refreshToken` non-empty).
- `src/services/auth.service.js`:
  - `login({ username, password })`:
    1. `userRepository.findByUsername(username)` → throw `InvalidCredentialsError` on miss (note: same error for "user not found" and "wrong password" to avoid username enumeration).
    2. `password.verify(user.password_hash, password)` → throw `InvalidCredentialsError` on false.
    3. **If `password.needsRehash(user.password_hash)`** → re-hash with current config and `userRepository.updatePasswordHash(...)`.
    4. Return `{ accessToken, refreshToken, tokenType, accessTokenExpiresInSeconds, refreshTokenExpiresInSeconds }` matching spec §5.1.
  - `refresh({ refreshToken })` — verify; re-resolve user (and **fresh roles**, spec §6.3 note); issue new access token; throw `InvalidRefreshTokenError` on miss.
  - `me({ accessToken })` — verify; return profile matching spec §5.3.
- `src/middleware/auth-required.js` — extract Bearer token; verify access token; attach `req.user`; throw `InvalidAccessTokenError` on miss.
- `src/controllers/auth.controller.js` — three handlers wiring zod validation → service → response. Validation failures throw `ValidationError`.
- `src/routes/auth.routes.js` — POST `/auth/login`, POST `/auth/refresh`, GET `/auth/me` (guarded).
- Mount under `/api/v1/auth/*`.
- `tests/integration/auth.login.test.js`, `auth.refresh.test.js`, `auth.me.test.js`:
  - Happy path for each of three seeded users.
  - 400 missing/empty fields.
  - 401 wrong password, malformed token, expired token, refresh-token-as-access, access-token-as-refresh.
  - All error responses asserted to be Problem Details shape.
  - Rehash-on-login: pre-seed a user with a hash made at deliberately weak params; assert that after a successful login, `password_hash` in the DB has been updated and starts with current params (`m=65536,t=2,p=1`).

---

### Phase 6 — Rate Limiting (Configurable)
**Goal:** `express-rate-limit` toggled by env, default off.

- `src/middleware/rate-limit.js` — exports a factory that returns either the limiter or a no-op based on `RATE_LIMIT_ENABLED`.
- Apply in `app.js` to `/api/v1/auth/login` and `/api/v1/auth/refresh` only (the abuse-prone endpoints).
- 429 responses also use Problem Details shape.
- Banner shows "Rate limiting: disabled" or "enabled (window: 60000ms, max: 100)".
- Integration test: with limiter enabled and threshold `max=2`, third login attempt returns 429 in Problem Details shape.

---

### Phase 7 — OpenAPI 3.1 + Swagger UI
**Goal:** Browsable, accurate API docs.

- `src/openapi/openapi.yaml` — hand-written 3.1 spec covering all four endpoints, request/response schemas mirroring spec §5, Problem Details schema as a reusable component, security scheme `bearerAuth: { type: http, scheme: bearer, bearerFormat: JWT }`.
- Loaded via `js-yaml` at boot; served by `swagger-ui-express` at `/api/docs`.
- `/api/openapi.json` returns the JSON form for tooling.
- Banner shows "API docs: http://0.0.0.0:5003/api/docs".
- Integration test: `GET /api/openapi.json` returns 200 and `info.version === package.json.version`.

---

### Phase 8 — Documentation & Final Polish
**Goal:** Ship-ready.

- `README.md` — what the service is; prerequisites (Node 22); install + run; env reference table; endpoint summary linking to spec; link to `/api/docs`; seeded credentials note.
- `CONTRIBUTING.md` — branch naming; commit style; how to run tests + coverage; code layout overview.
- `CHANGELOG.md` — single entry `0.1.0 — Initial implementation of /auth/login, /auth/refresh, /auth/me, /health with Argon2id password hashing`.
- `TROUBLESHOOTING.md` — common issues:
  - Port `5003` already in use.
  - Signing key mismatch with the gateway.
  - SQLite file locked.
  - **Argon2 native build fails on Windows** (node-gyp / C++ toolchain). Workarounds: (a) install windows-build-tools / VS Build Tools; (b) swap `argon2` for `@node-rs/argon2` (drop-in API for `hash`/`verify`; for `needsRehash` use `hash-wasm` or implement a small parser of the PHC string's `m=`, `t=`, `p=` parameters); (c) `hash-wasm` as a pure-WASM fallback.
  - `RATE_LIMIT_ENABLED=true` blocking integration tests — keep disabled in test env.
- Coverage target: ≥80% statements (informational, not gating).
- Final smoke run: `npm start` then exercise each endpoint manually with the three seeded credentials.

---

## 9. Verification Plan

Each phase ends with a concrete check before moving on:

| Phase | Verification                                                                                                   |
| ----- | -------------------------------------------------------------------------------------------------------------- |
| 1     | `npm start` shows banner; `curl /` returns 404 in JSON.                                                        |
| 2     | `curl /api/v1/health` returns spec-shaped 200; bad route returns Problem Details 404.                          |
| 3     | Fresh boot seeds 3 users; second boot reports "seed skipped". DB rows show PHC strings starting with `$argon2id$`. |
| 4     | `npm test` — all unit tests for token + password green; `needsRehash` weak-params test green.                 |
| 5     | `npm run test:int` — happy + error paths green for all three auth endpoints; rehash-on-login test green.       |
| 6     | With `RATE_LIMIT_ENABLED=true RATE_LIMIT_MAX=2`, third login → 429 Problem Details.                            |
| 7     | Browser shows Swagger UI at `/api/docs` rendering all four endpoints; "Try it out" succeeds for `/health`.     |
| 8     | Manual end-to-end: login as `buyer.user`; decode JWT to confirm `iss`, `aud`, `tokenType`, `roles`; refresh to get new access token; `/auth/me` returns matching profile. |

---

## 10. Out of Plan (Deferred)

Per [Auth_Service_Specification.md §13](Auth_Service_Specification.md#13-out-of-scope) the following are explicitly excluded and will **not** be implemented:
- User registration and self-service password reset.
- Multi-factor authentication (MFA / 2FA).
- SSO / OIDC federation (SAML, external IdP).
- Account lockout, password complexity policy, password expiry.
- Audit logging of authentication events at the Auth Service layer.
- Refresh token rotation, denylist, or revocation endpoints.
- Token introspection (`/introspect`).
- OpenID Connect Discovery (`/.well-known/openid-configuration`).
- JWKS endpoint (only relevant for asymmetric signing).
- Multi-tenant or multi-organisation support.
- Per-service token re-validation (deferred per ADR 0006).

Risks acknowledged and accepted for this build (per spec §12):
- Secrets in `.env` (S1 — production: secrets manager).
- HS256 symmetric signing (S2 — production: RS256 with public-key distribution).
- No server-side logout / refresh-token revocation (S3).
- HTTPS termination assumed at the perimeter (S4).
- No rate limiting / brute-force protection by default (S5 — toggled via env when needed).

---

### Document History

| Version | Date        | Author    | Notes                                                                       |
| ------- | ----------- | --------- | --------------------------------------------------------------------------- |
| 0.1     | 05 May 2026 | Ramkumar  | Initial implementation plan; Argon2id locked per architecture review.       |

---

*End of Document*

# Auth Service

RFx Sourcing — Authentication Service. Issues HS256-signed JWTs consumed by the API Gateway and client applications.

## Prerequisites

- Node.js 22+
- npm 10+

## Quick Start

```powershell
# 1. Install dependencies
npm install

# 2. Copy env template and set your signing key
Copy-Item .env.example .env
# Edit .env — set JWT_SIGNING_KEY to a secure random string (min 32 chars)

# 3. Start the server
npm start

# 4. Dev mode (auto-reload on file change)
npm run dev
```

The server binds to `0.0.0.0:5003` by default. API docs are available at `http://localhost:5003/api/docs`.

## Seeded Credentials (development only)

| Username        | Password       | Roles              |
| --------------- | -------------- | ------------------ |
| `buyer.user`    | `Buyer@123`    | `["Buyer"]`        |
| `supplier.user` | `Supplier@123` | `["Supplier"]`     |
| `admin.user`    | `Admin@123`    | `["Admin", "All"]` |

## Endpoints

| Method | Path                    | Auth             | Description                        |
| ------ | ----------------------- | ---------------- | ---------------------------------- |
| POST   | `/api/v1/auth/login`    | None             | Authenticate; receive tokens       |
| POST   | `/api/v1/auth/refresh`  | Refresh token    | Exchange refresh for access token  |
| GET    | `/api/v1/auth/me`       | Bearer token     | Current user profile               |
| GET    | `/api/v1/health`        | None             | Liveness probe                     |

Full contract: [docs/Auth_Service_Specification.md](docs/Auth_Service_Specification.md)

## Configuration

Copy `.env.example` to `.env` and adjust as needed. Every variable is validated at startup — the service exits immediately with a clear error if a required value is missing or invalid.

| Variable | Default | Description |
| -------- | ------- | ----------- |
| `APP_NAME` | `auth-service` | Service name used in logs and the health response |
| `NODE_ENV` | `development` | `development` \| `production` \| `test` |
| `HOST` | `0.0.0.0` | Bind address |
| `PORT` | `5003` | Bind port — must not conflict with the API Gateway (`5000`) |
| `LOG_LEVEL` | `info` | Pino level: `fatal` \| `error` \| `warn` \| `info` \| `debug` \| `trace` |
| `JWT_SIGNING_KEY` | *(required)* | HS256 secret — **min 32 chars, must match the Gateway's `Jwt:SigningKey`** |
| `JWT_ISSUER` | `rfx-auth-service` | JWT `iss` claim — must match the Gateway's expected issuer |
| `JWT_AUDIENCE` | `rfx-sourcing-system` | JWT `aud` claim — must match the Gateway's expected audience |
| `JWT_ACCESS_TOKEN_LIFETIME_SECONDS` | `900` | Access token TTL (15 min) |
| `JWT_REFRESH_TOKEN_LIFETIME_SECONDS` | `86400` | Refresh token TTL (24 h) |
| `DATABASE_PATH` | `./data/auth.db` | SQLite file path; directory is created automatically |
| `SEED_DATA_ENABLED` | `true` | Insert the three dev users on first boot; skipped if table is non-empty |
| `ARGON2_MEMORY_COST_KIB` | `65536` | Argon2id memory cost (64 MiB); minimum 19456 |
| `ARGON2_TIME_COST` | `2` | Argon2id iterations |
| `ARGON2_PARALLELISM` | `1` | Argon2id parallelism |
| `ARGON2_HASH_LENGTH` | `32` | Argon2id output length in bytes |
| `RATE_LIMIT_ENABLED` | `false` | Set `true` to enable rate limiting on `/auth/login` and `/auth/refresh` |
| `RATE_LIMIT_WINDOW_MS` | `60000` | Rate-limit window in milliseconds |
| `RATE_LIMIT_MAX` | `100` | Max requests per IP per window |

## Testing

```powershell
npm test              # unit tests
npm run test:int      # integration tests
npm run test:coverage # with coverage report
```

## Architecture

- [docs/Auth_Service_Specification.md](docs/Auth_Service_Specification.md) — API contract
- [docs/auth-service-impl-plan.md](docs/auth-service-impl-plan.md) — implementation plan

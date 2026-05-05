# Contributing

## Branch Naming

| Type    | Pattern                  | Example                        |
| ------- | ------------------------ | ------------------------------ |
| Feature | `feat/<short-desc>`      | `feat/auth-login-endpoint`     |
| Fix     | `fix/<short-desc>`       | `fix/token-expiry-validation`  |
| Docs    | `docs/<short-desc>`      | `docs/troubleshooting-argon2`  |
| Test    | `test/<short-desc>`      | `test/refresh-token-edge-cases`|

## Commit Style

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): short imperative message

feat(auth): add POST /auth/login endpoint
fix(token): reject refresh tokens used as access tokens
docs(readme): add seeded credentials table
test(password): add needsRehash weak-params test
```

## Running Tests

```powershell
npm test              # unit tests only (fast)
npm run test:int      # integration tests (starts in-process Express)
npm run test:coverage # full run with V8 coverage report
```

Keep `RATE_LIMIT_ENABLED=false` in your test `.env` to avoid 429s during integration tests.

## Code Layout

See [docs/auth-service-impl-plan.md](docs/auth-service-impl-plan.md) §5 for the full folder structure and module responsibilities.

Key conventions:
- All source under `src/`. Entry point is `src/server.js`.
- `src/app.js` exports `createApp()` — no `listen()` call, keeps it testable.
- Business logic lives in `src/services/`. Controllers are thin wrappers.
- All errors throw an `AppError` subclass; the global handler in `src/middleware/error-handler.js` maps them to RFC 7807 Problem Details.
- `.env` is never committed. `.env.example` is the canonical reference.

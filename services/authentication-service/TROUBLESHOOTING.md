# Troubleshooting

## Port 5003 Already in Use

Find and kill the conflicting process:

```powershell
# Find PID holding port 5003
netstat -ano | findstr :5003

# Stop it
Stop-Process -Id <PID> -Force
```

Or change `PORT` in your `.env`.

---

## Server Fails to Start — JWT_SIGNING_KEY Too Short

```
Invalid environment configuration:
{ JWT_SIGNING_KEY: [ 'JWT_SIGNING_KEY must be at least 32 characters' ] }
```

Copy `.env.example` to `.env` and set a real key:

```powershell
Copy-Item .env.example .env
# Then edit .env and replace the placeholder JWT_SIGNING_KEY value
```

---

## Argon2 Native Build Fails on Windows

The `argon2` npm package requires a C++ toolchain via node-gyp. On Windows this can fail if VS Build Tools are not installed.

**Option A — Install VS Build Tools (recommended for full native support)**

1. Download [VS Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/)
2. Install with "Desktop development with C++" workload
3. Re-run `npm install`

**Option B — Switch to @node-rs/argon2 (Rust-based, no node-gyp)**

```powershell
npm uninstall argon2
npm install @node-rs/argon2
```

Update `src/services/password.service.js`:
- Replace `import argon2 from 'argon2'` with `import * as argon2 from '@node-rs/argon2'`
- `@node-rs/argon2` does not expose `needsRehash` — implement it by parsing the PHC string parameters (`m=`, `t=`, `p=`) and comparing against current config.

**Option C — hash-wasm (pure WebAssembly, zero native build)**

```powershell
npm uninstall argon2
npm install hash-wasm
```

Requires rewriting `src/services/password.service.js` using the `hash-wasm` Argon2id API. PHC-format output must be assembled manually.

---

## better-sqlite3 Build Fails on Windows

Same root cause as argon2 — requires node-gyp.

```powershell
npm install --global windows-build-tools
npm install
```

Or install VS Build Tools as in Option A above.

---

## SQLite Database Locked

```
SqliteError: database is locked
```

Another process (or a crashed test run) is holding the database open.

```powershell
# Find and stop any node processes
Get-Process node | Stop-Process -Force

# Or delete the database and let the service re-seed
Remove-Item .\data\auth.db -ErrorAction SilentlyContinue
```

---

## Signing Key Mismatch with Gateway

Symptom: every API call through the gateway returns `401 invalid-signature`.

Cause: `JWT_SIGNING_KEY` in this service's `.env` does not match the gateway's `Jwt:SigningKey` in `appsettings.json`.

Fix: copy the exact same string to both configs. Any whitespace difference will cause a mismatch.

---

## RATE_LIMIT_ENABLED=true Blocking Integration Tests

Integration tests hammer endpoints rapidly. With rate limiting enabled they hit the 429 threshold.

Fix: keep your test environment `.env` (or `NODE_ENV=test` override) with `RATE_LIMIT_ENABLED=false`.

---

## Seed Not Running on Startup

Check:
1. `SEED_DATA_ENABLED` is not `false` in your `.env`.
2. The `users` table is empty — seed is skipped when the table already has rows (idempotent).
3. To force a re-seed, delete `data/auth.db` and restart.

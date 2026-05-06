# Troubleshooting

Common issues when running the Ticket Management MCP Server locally or in Docker.

---

## Server fails to start

### `.env` file not found

**Symptom:** `pydantic_settings.env_settings.EnvSettingsError` or settings fall back to defaults unexpectedly.

**Fix:**

```powershell
Copy-Item .env.example .env
```

Then edit `.env` if you need non-default values.

---

### Port 8989 already in use

**Symptom:** `OSError: [Errno 98] Address already in use` or `Only one usage of each socket address is normally permitted`.

**Fix (Windows):**

```powershell
Get-NetTCPConnection -LocalPort 8989 | Select-Object OwningProcess
Stop-Process -Id <pid>
```

Or change `MCP_PORT` in `.env` to a free port.

---

### `data/` directory does not exist

**Symptom:** `sqlite3.OperationalError: unable to open database file`.

**Cause:** The `data/` directory was not created (e.g. after a fresh clone without the `.gitkeep`).

**Fix:**

```powershell
New-Item -ItemType Directory -Path data -Force
uv run python server.py
```

---

### `ModuleNotFoundError: No module named 'ticket_mgmt'`

**Symptom:** Import error when running `python server.py` directly without `uv run`.

**Fix:** Always run via `uv run` so the virtual environment is activated:

```powershell
uv run python server.py
```

Or activate the environment manually:

```powershell
.\.venv\Scripts\Activate.ps1
python server.py
```

---

## Database issues

### SQLite database is locked

**Symptom:** `sqlalchemy.exc.OperationalError: (sqlite3.OperationalError) database is locked`.

**Cause:** Multiple server instances or processes are accessing the same SQLite file simultaneously.

**Fix:** Ensure only one instance of the server runs at a time. Stop any other Python processes accessing `data/tickets.db`.

---

### Ticket IDs restart from TKT10001 after deleting the database

**Expected behaviour:** Deleting `data/tickets.db` resets the ID counter — the next ticket will be `TKT10001`. This is by design; the counter is seeded on `init_db()`.

---

## Docker issues

### Volume permission error inside the container

**Symptom:** `PermissionError: [Errno 13] Permission denied: '/app/data/tickets.db'`.

**Cause:** The `./data` host directory is owned by root and the container runs as `appuser` (UID 1001).

**Fix (Windows — Docker Desktop):** Usually not an issue. On Linux hosts:

```bash
sudo chown -R 1001:1001 ./data
```

---

### Changes to `.env` not picked up after `docker compose up`

**Cause:** Docker Compose caches the environment at container start.

**Fix:**

```powershell
docker compose down
docker compose up
```

---

### Container exits immediately

**Fix:** Check the logs:

```powershell
docker compose logs ticket-mcp
```

Common causes: missing `.env` file, `data/` directory not writable, or a Python import error.

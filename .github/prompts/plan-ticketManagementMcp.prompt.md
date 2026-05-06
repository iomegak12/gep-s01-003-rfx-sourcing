## Plan: Ticket Management FastMCP Server

Build `ticket-management-mcp` — a FastMCP-based Python MCP server (Streamable HTTP on `0.0.0.0:8989`) that tracks support tickets across domains, persisted to SQLite via SQLAlchemy 2.x, packaged as a `src/ticket_mgmt` module and shipped with multi-stage Docker.

**Confirmed decisions**

| Area | Decision |
|---|---|
| Python / runtime | 3.12, uv-managed, package `src/ticket_mgmt/` |
| Transport | Streamable HTTP, `0.0.0.0:8989` |
| DB | SQLite via SQLAlchemy 2 (sync), `create_all` on startup, path from `.env` |
| Console | `rich` startup banner + tables for tools/resources/prompts |
| Priorities | LOW, MEDIUM, HIGH, CRITICAL (default MEDIUM) |
| Statuses | OPEN, IN_PROGRESS, RESOLVED, CLOSED |
| Lifecycle | register→OPEN; resolve from {OPEN,IN_PROGRESS}→RESOLVED; close from RESOLVED→CLOSED |
| Ticket ID | `TKT10001…` via persistent counter table |
| Domain | optional free-text column |
| Search | AND across optional filters; description=partial CI; status/priority=exact CI |
| Person lookup | one tool with `role: 'any'|'raised_by'|'resolved_by'` |

**Phases & steps**

**Phase 1 — Scaffolding**
1. Update `pyproject.toml` (deps: `fastmcp`, `sqlalchemy>=2`, `pydantic-settings`, `python-dotenv`, `rich`) and configure `src/ticket_mgmt` package layout.
2. Add `.gitignore`, `.dockerignore`, `.env.example`, `LICENSE` (MIT), `data/.gitkeep`.

**Phase 2 — Domain & persistence** (sequential)

3. `enums.py` — `Priority`, `Status` as `str, Enum`.
4. `config.py` — `pydantic_settings.BaseSettings` (database_url, mcp_host, mcp_port, log_level), cached.
5. `db/models.py` — `Ticket` and `IdSequence` (seeded `('ticket', 10001)`).
6. `db/engine.py` — engine + `init_db()` ensures dir, creates tables, seeds counter.
7. `repositories/ticket_repo.py` — atomic `next_ticket_id()`, CRUD, search, by-person, stats, recent, by-status.
8. `services/ticket_service.py` — business rules; invalid transitions raise `ToolError`.

**Phase 3 — MCP layer** (parallel after Phase 2)

9. `tools.py` — 5 tools: `register_ticket`, `get_tickets_by_person`, `search_tickets`, `resolve_ticket`, `close_ticket`. Read tools get `readOnlyHint=True`. Structured returns via Pydantic models.
10. `resources.py` — `tickets://stats`, `tickets://recent{?limit}`, `tickets://by-status/{status}`.
11. `prompts.py` — `summarize_open_tickets`, `daily_standup_report(person_name)`, `triage_new_ticket(description)`.
12. `console.py` — rich banner: name/version/transport URL/DB path + tool/resource/prompt tables.
13. `mcp_app.py` — `create_app()` builds FastMCP, runs `init_db()`, registers everything, prints banner.

**Phase 4 — Entrypoint & containerization**

14. `server.py` — minimal (~10 lines): import, build, `mcp.run(transport="http", host=..., port=...)`.
15. `Dockerfile` — multi-stage `python:3.12-slim`; builder uses `uv sync --frozen`, runtime copies `.venv` + source, non-root, `EXPOSE 8989`, no HEALTHCHECK.
16. `.dockerignore` — venv, caches, db, .env, .git.
17. `docker-compose.yml` — port `8989:8989`, `env_file: .env`, volume `./data:/app/data`.

**Phase 5 — Docs** (parallel)

18-21. `README.md`, `CONTRIBUTING.md`, `TROUBLESHOOTING.md`, `CHANGELOG.md` (Keep-a-Changelog, 0.1.0 entry).

**Relevant files**
- [mcp_servers/ticket-management-mcp/pyproject.toml](mcp_servers/ticket-management-mcp/pyproject.toml) — existing uv manifest to extend.
- [services/event-service/README.md](services/event-service/README.md), [services/event-service/CONTRIBUTING.md](services/event-service/CONTRIBUTING.md), [services/event-service/TROUBLESHOOTING.md](services/event-service/TROUBLESHOOTING.md), [services/event-service/CHANGELOG.md](services/event-service/CHANGELOG.md) — reference style for sibling docs in this repo.

**Verification**
1. `uv sync` → `uv run python server.py` — banner shows `http://0.0.0.0:8989/mcp` + DB path; `./data/tickets.db` created.
2. MCP Inspector at `http://localhost:8989/mcp` — verify 5 tools / 3 resources (incl. template) / 3 prompts.
3. Functional smoke: register two tickets → `TKT10001`, `TKT10002`; `search_tickets(priority="high")` (lowercase) returns matches; `resolve_ticket("TKT10001","Alice Smith")` → RESOLVED; `close_ticket("TKT10001")` → CLOSED; `close_ticket("TKT10002")` → ToolError; read `tickets://stats` and `tickets://by-status/CLOSED`.
4. `docker compose up --build` — same checks; restart container — data persists via volume.

**Decisions / Scope**
- **In:** confirmed decisions table above.
- **Out:** auth, attachments, Alembic migrations, Docker HEALTHCHECK, automated tests, OpenTelemetry.

**Further considerations**
1. **README server-holder name** — for `LICENSE` and `README` author/copyright line, default to `Ramkumar` for 2026. Confirm or specify an org/different name.
2. **`registered_date` source** — default to UTC `datetime.utcnow()` server-side. Allow LLM to pass a custom value? Recommendation: **server-side only** (cleaner audit). Confirm.

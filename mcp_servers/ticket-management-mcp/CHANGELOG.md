# Changelog

All notable changes to the Ticket Management MCP Server are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] – 2026-05-06

### Added — Initial release

- `src/ticket_mgmt/` Python package with modular layered structure (tools → services → repositories → DB models).
- `enums.py` — `Priority` (LOW, MEDIUM, HIGH, CRITICAL) and `Status` (OPEN, IN_PROGRESS, RESOLVED, CLOSED) enumerations.
- `config.py` — `pydantic-settings`-based `Settings` loaded from `.env`; configures `DATABASE_URL`, `MCP_HOST`, `MCP_PORT`, `LOG_LEVEL`.
- `db/models.py` — SQLAlchemy 2 ORM models: `Ticket` (nine fields) and `IdSequence` (persistent counter seeded at 10001).
- `db/engine.py` — engine factory, `SessionLocal`, and `init_db()` (auto-creates tables and seeds counter).
- `repositories/ticket_repo.py` — atomic `next_ticket_id()`, CRUD, `find_by_person`, `search`, `count_by_status_and_priority`, `recent`, `by_status`.
- `services/ticket_service.py` — business rules: `register_ticket`, `resolve_ticket` (OPEN/IN_PROGRESS → RESOLVED), `close_ticket` (RESOLVED → CLOSED); invalid transitions raise `ToolError`.
- **5 MCP Tools** via `tools.py`: `register_ticket`, `get_tickets_by_person`, `search_tickets`, `resolve_ticket`, `close_ticket`. Read tools annotated with `readOnlyHint=True`. All tools return structured Pydantic output.
- **3 MCP Resources** via `resources.py`: `tickets://stats` (JSON counts), `tickets://recent{?limit}` (newest N), `tickets://by-status/{status}` (template).
- **3 MCP Prompts** via `prompts.py`: `summarize_open_tickets`, `daily_standup_report`, `triage_new_ticket`.
- `console.py` — `rich`-based startup banner (server name, version, transport URL, DB path) and component registration table.
- `mcp_app.py` — `create_app()` factory: initialises DB, builds `FastMCP` instance, registers all components, prints banner.
- `server.py` — minimal entrypoint (~9 lines); runs Streamable HTTP transport on configured host/port.
- Multi-stage `Dockerfile` (`python:3.12-slim`, `uv sync --frozen`, non-root `appuser`, `EXPOSE 8989`, no HEALTHCHECK).
- `docker-compose.yml` — service `ticket-mcp`, port `8989:8989`, `env_file: .env`, volume `./data:/app/data`.
- `.gitignore`, `.dockerignore`, `.env.example`, `LICENSE` (MIT), `data/.gitkeep`.
- `README.md`, `CONTRIBUTING.md`, `TROUBLESHOOTING.md`, `CHANGELOG.md`.

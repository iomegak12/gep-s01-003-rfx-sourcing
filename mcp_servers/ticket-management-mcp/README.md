# Ticket Management MCP Server

A [FastMCP](https://gofastmcp.com)-based Python MCP server that tracks support tickets across service domains. Tickets are persisted to SQLite via SQLAlchemy 2 and the server exposes tools, resources, and prompts over Streamable HTTP on port `8989`.

---

## Features

- **5 MCP Tools** — register, search, look up, resolve, and close tickets.
- **3 MCP Resources** — live stats, recent tickets, and status-filtered ticket lists.
- **3 MCP Prompts** — open-ticket summary, daily stand-up report, and new-ticket triage.
- **Persistent SQLite storage** — path is configurable via `.env`.
- **Auto-incremented ticket IDs** — `TKT10001`, `TKT10002`, …
- **Rich startup console** — colourful banner with transport URL, DB path, and component tables.
- **Multi-stage Docker build** — production-ready, non-root container.

---

## Prerequisites

- Python 3.12 +
- [uv](https://docs.astral.sh/uv/) (already initialised in this folder)
- Docker + Docker Compose (optional, for containerised deployment)

---

## Quick Start (local, uv)

```powershell
# 1. Copy the environment template
Copy-Item .env.example .env

# 2. Install dependencies
uv sync

# 3. Start the server
uv run python server.py
```

The server starts at `http://0.0.0.0:8989/mcp`. Connect any MCP client (e.g. MCP Inspector, Claude Desktop) to that URL.

---

## Quick Start (Docker Compose)

```powershell
# Copy the environment template (if not already done)
Copy-Item .env.example .env

# Build and start
docker compose up --build
```

The SQLite database is persisted in `./data/` via a volume mount — data survives container restarts.

---

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `DATABASE_URL` | `sqlite:///./data/tickets.db` | SQLAlchemy database URL |
| `MCP_HOST` | `0.0.0.0` | Bind address for the HTTP transport |
| `MCP_PORT` | `8989` | Listening port |
| `LOG_LEVEL` | `INFO` | Python logging level |

---

## Ticket Schema

| Field | Type | Notes |
|---|---|---|
| `ticket_id` | string | Auto-generated: `TKT10001`, `TKT10002`, … |
| `description` | string | Full description of the issue |
| `raised_by` | string | Full name of the person raising the ticket |
| `registered_date` | ISO datetime | Set server-side (UTC) at registration |
| `priority` | enum | `LOW` \| `MEDIUM` \| `HIGH` \| `CRITICAL` |
| `status` | enum | `OPEN` \| `IN_PROGRESS` \| `RESOLVED` \| `CLOSED` |
| `domain` | string (optional) | Service domain, e.g. `event-service` |
| `resolved_by` | string (optional) | Name of person / team who resolved it |
| `resolved_date` | ISO datetime (optional) | Set server-side (UTC) at resolution |

### Status lifecycle

```
OPEN ──► IN_PROGRESS ──► RESOLVED ──► CLOSED
  └────────────────────────────────►
```

- `resolve_ticket` is allowed from `OPEN` or `IN_PROGRESS`.
- `close_ticket` is allowed only from `RESOLVED`.

---

## MCP Tools

| Tool | Description |
|---|---|
| `register_ticket` | Create a new ticket. Returns the full ticket record with its assigned ID. |
| `get_tickets_by_person` | Find tickets by person name (partial, case-insensitive). Use `role` to narrow to `raised_by`, `resolved_by`, or `any`. |
| `search_tickets` | Filter tickets by `description` (partial CI), `status`, and/or `priority` (AND semantics). All parameters optional. |
| `resolve_ticket` | Mark a ticket RESOLVED. Captures `resolved_by` and `resolved_date`. Requires current status to be OPEN or IN_PROGRESS. |
| `close_ticket` | Mark a ticket CLOSED. Requires current status to be RESOLVED. |

---

## MCP Resources

| URI | Description |
|---|---|
| `tickets://stats` | JSON ticket counts grouped by status and priority, plus total. |
| `tickets://recent?limit=N` | The N most recently registered tickets (newest first). Default `limit=10`. |
| `tickets://by-status/{status}` | All tickets for the specified status (OPEN, IN_PROGRESS, RESOLVED, or CLOSED). |

---

## MCP Prompts

| Prompt | Parameters | Description |
|---|---|---|
| `summarize_open_tickets` | — | Generates a prompt to summarize all OPEN / IN_PROGRESS tickets, grouped by priority. |
| `daily_standup_report` | `person_name` | Generates a stand-up conversation for a specific person's open and recently resolved tickets. |
| `triage_new_ticket` | `description` | Asks the LLM to recommend a priority, domain, and concise title for a new ticket. |

---

## Project Structure

```
ticket-management-mcp/
├── server.py                   # Minimal entrypoint (~10 lines)
├── src/
│   └── ticket_mgmt/
│       ├── config.py           # Settings from .env
│       ├── console.py          # Rich startup banner
│       ├── enums.py            # Priority, Status
│       ├── mcp_app.py          # FastMCP factory
│       ├── tools.py            # 5 MCP tools
│       ├── resources.py        # 3 MCP resources
│       ├── prompts.py          # 3 MCP prompts
│       ├── db/
│       │   ├── engine.py       # SQLAlchemy engine + init_db()
│       │   └── models.py       # ORM models (Ticket, IdSequence)
│       ├── repositories/
│       │   └── ticket_repo.py  # Data access
│       └── services/
│           └── ticket_service.py  # Business rules
├── data/                       # SQLite database (gitignored)
├── Dockerfile                  # Multi-stage, python:3.12-slim
├── docker-compose.yml
├── pyproject.toml
└── .env.example
```

---

## License

MIT © 2026 Ramkumar

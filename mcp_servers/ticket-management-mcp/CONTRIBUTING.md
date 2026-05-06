# Contributing

Thank you for considering a contribution to the Ticket Management MCP Server.

## Branching

- `main` — protected; merged only via pull request after review.
- Feature branches — `feature/<short-description>` (e.g. `feature/bulk-resolve`).
- Fix branches — `fix/<short-description>`.

## Commit style

Conventional commits, lowercase:

```
feat(tools): add bulk-resolve tool
fix(service): reject close from IN_PROGRESS status
docs(readme): add docker compose section
chore(deps): bump fastmcp to 2.4.0
```

## Pull request checklist

Before requesting review:

- [ ] `uv sync` succeeds with no errors.
- [ ] `uv run python server.py` starts cleanly and the banner displays.
- [ ] New behaviour is covered by manual smoke tests (MCP Inspector or equivalent).
- [ ] No secrets, no `.env` files, no `*.db` files committed.
- [ ] `CHANGELOG.md` updated under `## [Unreleased]`.

## Architecture rules

Follow the four-layer separation:

```
MCP Layer (tools/resources/prompts) → Service → Repository → DB Models
```

- **Tools / Resources / Prompts** — bind MCP inputs, delegate to a service or repository, return structured output. No business logic.
- **Services** — enforce all business rules and lifecycle transitions. Raise `ToolError` for invalid operations.
- **Repositories** — data access only. Return ORM model instances; no business logic.
- **DB Models** — SQLAlchemy ORM definitions and the `IdSequence` counter. No behaviour.

## Adding a new tool

1. Add any new ORM columns to `src/ticket_mgmt/db/models.py` and test `init_db()` creates them.
2. Add the data-access method to `src/ticket_mgmt/repositories/ticket_repo.py`.
3. Add business logic (if any) to `src/ticket_mgmt/services/ticket_service.py`.
4. Register the tool in `src/ticket_mgmt/tools.py` inside `register_all()`.
5. Add the tool name to `_TOOL_NAMES` in `src/ticket_mgmt/mcp_app.py`.
6. Update `README.md` tool table and `CHANGELOG.md`.

## Development setup

```powershell
# Clone the repo and navigate to this service
cd mcp_servers\ticket-management-mcp

# Install all dependencies
uv sync

# Copy and configure the environment
Copy-Item .env.example .env

# Run the server
uv run python server.py
```

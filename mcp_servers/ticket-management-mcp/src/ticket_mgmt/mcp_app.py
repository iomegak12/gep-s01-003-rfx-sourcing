"""Factory that assembles the FastMCP application."""

from __future__ import annotations

from fastmcp import FastMCP

from ticket_mgmt import prompts, resources, tools
from ticket_mgmt.config import Settings, get_settings
from ticket_mgmt.console import print_components_table, print_startup_banner
from ticket_mgmt.db.engine import init_db
from ticket_mgmt.seed import seed_sample_tickets

_TOOL_NAMES = [
    "register_ticket",
    "get_tickets_by_person",
    "search_tickets",
    "resolve_ticket",
    "close_ticket",
]
_RESOURCE_NAMES = [
    "tickets://stats",
    "tickets://recent{?limit}",
    "tickets://by-status/{status}",
]
_PROMPT_NAMES = [
    "summarize_open_tickets",
    "daily_standup_report",
    "triage_new_ticket",
]


def create_app() -> tuple[FastMCP, Settings]:
    """Initialise the database, build and return the FastMCP instance."""
    settings = get_settings()

    # Initialise database (create tables + seed counter if needed)
    init_db()

    # Seed sample tickets if the database is empty (configurable via SEED_ON_STARTUP)
    if settings.seed_on_startup:
        seeded = seed_sample_tickets(force=settings.seed_force)
        if seeded:
            from rich.console import Console
            Console().print(
                f"[bold green]✓[/bold green] Seeded [bold]{seeded}[/bold] sample tickets into the database."
            )

    mcp = FastMCP(
        name="Ticket Management MCP Server",
        instructions=(
            "This server tracks support tickets across multiple service domains. "
            "Use register_ticket to open a new ticket, resolve_ticket to mark it resolved "
            "(requires resolved_by), and close_ticket to close a resolved ticket. "
            "Use search_tickets to filter by description, status, or priority. "
            "Use get_tickets_by_person to find tickets by who raised or resolved them. "
            "Resources provide live stats, recent tickets, and tickets filtered by status."
        ),
        version="0.1.0",
    )

    # Register all MCP components
    tools.register_all(mcp)
    resources.register_all(mcp)
    prompts.register_all(mcp)

    # Print startup banner and component table
    print_startup_banner(
        host=settings.mcp_host,
        port=settings.mcp_port,
        db_url=settings.database_url,
        version="0.1.0",
    )
    print_components_table(
        tools=_TOOL_NAMES,
        resources=_RESOURCE_NAMES,
        prompts=_PROMPT_NAMES,
        host=settings.mcp_host,
        port=settings.mcp_port,
    )

    return mcp, settings

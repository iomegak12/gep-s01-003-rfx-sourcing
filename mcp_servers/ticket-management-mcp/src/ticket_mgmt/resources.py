"""MCP resources — read-only data sources exposed to LLM clients."""

from __future__ import annotations

import json

from fastmcp import FastMCP

from ticket_mgmt.repositories import ticket_repo


def register_all(mcp: FastMCP) -> None:

    @mcp.resource(
        "tickets://stats",
        name="TicketStats",
        description="Ticket counts grouped by status and priority, plus the total count.",
        mime_type="application/json",
        tags={"tickets", "stats"},
    )
    def ticket_stats() -> str:
        """Return a JSON summary of ticket counts by status and priority."""
        data = ticket_repo.count_by_status_and_priority()
        return json.dumps(data, indent=2)

    @mcp.resource(
        "tickets://recent{?limit}",
        name="RecentTickets",
        description="The most recently registered tickets, newest first. Optional ?limit parameter (default 10).",
        mime_type="application/json",
        tags={"tickets", "recent"},
    )
    def recent_tickets(limit: int = 10) -> str:
        """Return the N most recently registered tickets as JSON."""
        tickets = ticket_repo.recent(limit)
        result = [
            {
                "ticket_id": t.ticket_id,
                "description": t.description,
                "raised_by": t.raised_by,
                "registered_date": t.registered_date.isoformat() if t.registered_date else None,
                "priority": t.priority,
                "status": t.status,
                "domain": t.domain,
                "resolved_by": t.resolved_by,
                "resolved_date": t.resolved_date.isoformat() if t.resolved_date else None,
            }
            for t in tickets
        ]
        return json.dumps({"count": len(result), "tickets": result}, indent=2)

    @mcp.resource(
        "tickets://by-status/{status}",
        name="TicketsByStatus",
        description="All tickets for a given status (OPEN, IN_PROGRESS, RESOLVED, or CLOSED).",
        mime_type="application/json",
        tags={"tickets", "filter"},
    )
    def tickets_by_status(status: str) -> str:
        """Return all tickets with the specified status as JSON."""
        tickets = ticket_repo.by_status(status)
        result = [
            {
                "ticket_id": t.ticket_id,
                "description": t.description,
                "raised_by": t.raised_by,
                "registered_date": t.registered_date.isoformat() if t.registered_date else None,
                "priority": t.priority,
                "status": t.status,
                "domain": t.domain,
                "resolved_by": t.resolved_by,
                "resolved_date": t.resolved_date.isoformat() if t.resolved_date else None,
            }
            for t in tickets
        ]
        return json.dumps({"status_filter": status.upper(), "count": len(result), "tickets": result}, indent=2)

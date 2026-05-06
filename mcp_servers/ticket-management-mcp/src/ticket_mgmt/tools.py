"""MCP tools — the five callable capabilities exposed to LLM clients."""

from __future__ import annotations

from typing import Annotated, Literal

from fastmcp import FastMCP
from mcp.types import ToolAnnotations
from pydantic import BaseModel, Field

from ticket_mgmt.repositories import ticket_repo
from ticket_mgmt.services import ticket_service


# ---------------------------------------------------------------------------
# Shared response model
# ---------------------------------------------------------------------------

class TicketOut(BaseModel):
    ticket_id: str
    description: str
    raised_by: str
    registered_date: str
    priority: str
    status: str
    domain: str | None
    resolved_by: str | None
    resolved_date: str | None

    @classmethod
    def from_orm(cls, t) -> "TicketOut":
        return cls(
            ticket_id=t.ticket_id,
            description=t.description,
            raised_by=t.raised_by,
            registered_date=t.registered_date.isoformat() if t.registered_date else "",
            priority=t.priority,
            status=t.status,
            domain=t.domain,
            resolved_by=t.resolved_by,
            resolved_date=t.resolved_date.isoformat() if t.resolved_date else None,
        )


class TicketListOut(BaseModel):
    count: int
    tickets: list[TicketOut]


# ---------------------------------------------------------------------------
# Registration
# ---------------------------------------------------------------------------

def register_all(mcp: FastMCP) -> None:

    @mcp.tool(
        name="register_ticket",
        description=(
            "Register a new support ticket. "
            "Returns the created ticket with its auto-generated ID (e.g. TKT10001)."
        ),
        tags={"tickets", "write"},
    )
    def register_ticket(
        description: Annotated[str, Field(description="Full description of the issue.")],
        raised_by: Annotated[str, Field(description="Full name of the person raising the ticket.")],
        priority: Annotated[
            Literal["LOW", "MEDIUM", "HIGH", "CRITICAL"],
            Field(description="Ticket priority. Defaults to MEDIUM."),
        ] = "MEDIUM",
        domain: Annotated[
            str | None,
            Field(description="Optional service or domain name (e.g. event-service, bid-scoring-service)."),
        ] = None,
    ) -> TicketOut:
        """Register a new support ticket and return the full ticket record."""
        t = ticket_service.register_ticket(description, raised_by, priority, domain)
        return TicketOut.from_orm(t)

    @mcp.tool(
        name="get_tickets_by_person",
        description=(
            "Retrieve all tickets associated with a person by partial, case-insensitive name match. "
            "Use 'role' to narrow to tickets raised by or resolved by that person."
        ),
        tags={"tickets", "read"},
        annotations=ToolAnnotations(readOnlyHint=True, idempotentHint=True),
    )
    def get_tickets_by_person(
        name: Annotated[str, Field(description="Partial or full name to search for (case-insensitive).")],
        role: Annotated[
            Literal["any", "raised_by", "resolved_by"],
            Field(description="Which name field to match: 'any' (default), 'raised_by', or 'resolved_by'."),
        ] = "any",
    ) -> TicketListOut:
        """Return all tickets where the given name matches raised_by and/or resolved_by."""
        tickets = ticket_repo.find_by_person(name, role)
        return TicketListOut(count=len(tickets), tickets=[TicketOut.from_orm(t) for t in tickets])

    @mcp.tool(
        name="search_tickets",
        description=(
            "Filter / search tickets by description (partial, case-insensitive), "
            "status, and/or priority. All filters are AND-combined. All parameters are optional."
        ),
        tags={"tickets", "read"},
        annotations=ToolAnnotations(readOnlyHint=True, idempotentHint=True),
    )
    def search_tickets(
        description: Annotated[
            str | None,
            Field(description="Partial text to match against the ticket description (case-insensitive)."),
        ] = None,
        status: Annotated[
            str | None,
            Field(description="Filter by status: OPEN, IN_PROGRESS, RESOLVED, or CLOSED (case-insensitive)."),
        ] = None,
        priority: Annotated[
            str | None,
            Field(description="Filter by priority: LOW, MEDIUM, HIGH, or CRITICAL (case-insensitive)."),
        ] = None,
    ) -> TicketListOut:
        """Search tickets with optional AND-combined filters."""
        tickets = ticket_repo.search(description, status, priority)
        return TicketListOut(count=len(tickets), tickets=[TicketOut.from_orm(t) for t in tickets])

    @mcp.tool(
        name="resolve_ticket",
        description=(
            "Mark a ticket as RESOLVED. Captures who resolved it and the resolution timestamp. "
            "Only allowed when the current status is OPEN or IN_PROGRESS."
        ),
        tags={"tickets", "write"},
    )
    def resolve_ticket(
        ticket_id: Annotated[str, Field(description="The ticket ID to resolve (e.g. TKT10001).")],
        resolved_by: Annotated[
            str,
            Field(description="Full name of the person or team who resolved the ticket."),
        ],
    ) -> TicketOut:
        """Resolve an open ticket and record who resolved it."""
        t = ticket_service.resolve_ticket(ticket_id, resolved_by)
        return TicketOut.from_orm(t)

    @mcp.tool(
        name="close_ticket",
        description=(
            "Mark a ticket as CLOSED. "
            "Only allowed when the current status is RESOLVED."
        ),
        tags={"tickets", "write"},
    )
    def close_ticket(
        ticket_id: Annotated[str, Field(description="The ticket ID to close (e.g. TKT10001).")],
    ) -> TicketOut:
        """Close a resolved ticket."""
        t = ticket_service.close_ticket(ticket_id)
        return TicketOut.from_orm(t)

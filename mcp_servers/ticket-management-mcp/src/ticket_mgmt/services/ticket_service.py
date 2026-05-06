"""Ticket service — business rules and lifecycle transitions."""

from __future__ import annotations

from datetime import datetime, timezone

from fastmcp.exceptions import ToolError

from ticket_mgmt.db.models import Ticket
from ticket_mgmt.enums import Priority, Status
from ticket_mgmt.repositories import ticket_repo

# Statuses from which resolve is permitted
_RESOLVABLE = {Status.OPEN.value, Status.IN_PROGRESS.value}


def register_ticket(
    description: str,
    raised_by: str,
    priority: str = Priority.MEDIUM.value,
    domain: str | None = None,
) -> Ticket:
    """Create a new ticket, assign the next sequential ID, status=OPEN."""
    # Validate priority
    try:
        Priority(priority.upper())
    except ValueError:
        valid = ", ".join(p.value for p in Priority)
        raise ToolError(f"Invalid priority '{priority}'. Valid values: {valid}.")

    ticket_id = ticket_repo.next_ticket_id()
    now = datetime.now(tz=timezone.utc).replace(tzinfo=None)  # store as naive UTC

    ticket = Ticket(
        ticket_id=ticket_id,
        description=description,
        raised_by=raised_by,
        registered_date=now,
        priority=priority.upper(),
        status=Status.OPEN.value,
        domain=domain,
        resolved_by=None,
        resolved_date=None,
    )
    return ticket_repo.create(ticket)


def resolve_ticket(ticket_id: str, resolved_by: str) -> Ticket:
    """Set ticket status to RESOLVED. Only allowed from OPEN or IN_PROGRESS."""
    ticket = _get_or_raise(ticket_id)
    if ticket.status not in _RESOLVABLE:
        raise ToolError(
            f"Ticket '{ticket_id}' has status '{ticket.status}' and cannot be resolved. "
            f"Resolve is only allowed from: {', '.join(_RESOLVABLE)}."
        )
    now = datetime.now(tz=timezone.utc).replace(tzinfo=None)
    updated = ticket_repo.update_ticket(
        ticket_id,
        status=Status.RESOLVED.value,
        resolved_by=resolved_by,
        resolved_date=now,
    )
    return updated  # type: ignore[return-value]


def close_ticket(ticket_id: str) -> Ticket:
    """Set ticket status to CLOSED. Only allowed from RESOLVED."""
    ticket = _get_or_raise(ticket_id)
    if ticket.status != Status.RESOLVED.value:
        raise ToolError(
            f"Ticket '{ticket_id}' has status '{ticket.status}' and cannot be closed. "
            f"Close is only allowed from RESOLVED."
        )
    updated = ticket_repo.update_ticket(ticket_id, status=Status.CLOSED.value)
    return updated  # type: ignore[return-value]


# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

def _get_or_raise(ticket_id: str) -> Ticket:
    ticket = ticket_repo.get_by_id(ticket_id)
    if ticket is None:
        raise ToolError(f"Ticket '{ticket_id}' not found.")
    return ticket

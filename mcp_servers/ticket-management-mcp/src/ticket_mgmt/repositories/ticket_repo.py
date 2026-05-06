"""Ticket repository — all database access for tickets and the ID counter."""

from __future__ import annotations

from datetime import datetime
from typing import Literal

from sqlalchemy import func, select, update

from ticket_mgmt.db.engine import SessionLocal
from ticket_mgmt.db.models import IdSequence, Ticket


# ---------------------------------------------------------------------------
# ID generation
# ---------------------------------------------------------------------------

def next_ticket_id() -> str:
    """Atomically fetch-and-increment the ticket counter, return e.g. 'TKT10001'."""
    with SessionLocal() as session:
        # Lock the row for the duration of this transaction.
        row = (
            session.execute(
                select(IdSequence)
                .where(IdSequence.name == "ticket")
                .with_for_update()
            )
            .scalar_one()
        )
        current = row.next_value
        row.next_value = current + 1
        session.commit()
    return f"TKT{current}"


# ---------------------------------------------------------------------------
# CRUD
# ---------------------------------------------------------------------------

def create(ticket: Ticket) -> Ticket:
    with SessionLocal() as session:
        session.add(ticket)
        session.commit()
        session.refresh(ticket)
        return _detach(session, ticket)


def get_by_id(ticket_id: str) -> Ticket | None:
    with SessionLocal() as session:
        row = session.get(Ticket, ticket_id)
        return _copy(row) if row else None


def update_ticket(ticket_id: str, **fields) -> Ticket | None:
    with SessionLocal() as session:
        session.execute(
            update(Ticket).where(Ticket.ticket_id == ticket_id).values(**fields)
        )
        session.commit()
        row = session.get(Ticket, ticket_id)
        return _copy(row) if row else None


# ---------------------------------------------------------------------------
# Queries
# ---------------------------------------------------------------------------

def find_by_person(
    name: str,
    role: Literal["any", "raised_by", "resolved_by"] = "any",
) -> list[Ticket]:
    """Case-insensitive partial match on raised_by and/or resolved_by."""
    pattern = f"%{name.lower()}%"
    with SessionLocal() as session:
        stmt = select(Ticket)
        if role == "raised_by":
            stmt = stmt.where(func.lower(Ticket.raised_by).like(pattern))
        elif role == "resolved_by":
            stmt = stmt.where(func.lower(Ticket.resolved_by).like(pattern))
        else:
            from sqlalchemy import or_
            stmt = stmt.where(
                or_(
                    func.lower(Ticket.raised_by).like(pattern),
                    func.lower(Ticket.resolved_by).like(pattern),
                )
            )
        rows = session.execute(stmt).scalars().all()
        return [_copy(r) for r in rows]


def search(
    description: str | None = None,
    status: str | None = None,
    priority: str | None = None,
) -> list[Ticket]:
    """AND semantics across all provided filters."""
    with SessionLocal() as session:
        stmt = select(Ticket)
        if description is not None:
            stmt = stmt.where(
                func.lower(Ticket.description).like(f"%{description.lower()}%")
            )
        if status is not None:
            stmt = stmt.where(func.upper(Ticket.status) == status.upper())
        if priority is not None:
            stmt = stmt.where(func.upper(Ticket.priority) == priority.upper())
        rows = session.execute(stmt).scalars().all()
        return [_copy(r) for r in rows]


def count_by_status_and_priority() -> dict:
    """Return ticket counts grouped by status and priority for the stats resource."""
    with SessionLocal() as session:
        # By status
        status_rows = session.execute(
            select(Ticket.status, func.count().label("cnt")).group_by(Ticket.status)
        ).all()
        # By priority
        priority_rows = session.execute(
            select(Ticket.priority, func.count().label("cnt")).group_by(Ticket.priority)
        ).all()
        # Total
        total = session.execute(select(func.count()).select_from(Ticket)).scalar_one()
        return {
            "total": total,
            "by_status": {r.status: r.cnt for r in status_rows},
            "by_priority": {r.priority: r.cnt for r in priority_rows},
        }


def recent(limit: int = 10) -> list[Ticket]:
    """Return the most recently registered tickets, newest first."""
    with SessionLocal() as session:
        rows = (
            session.execute(
                select(Ticket)
                .order_by(Ticket.registered_date.desc())
                .limit(limit)
            )
            .scalars()
            .all()
        )
        return [_copy(r) for r in rows]


def by_status(status: str) -> list[Ticket]:
    with SessionLocal() as session:
        rows = (
            session.execute(
                select(Ticket).where(func.upper(Ticket.status) == status.upper())
            )
            .scalars()
            .all()
        )
        return [_copy(r) for r in rows]


# ---------------------------------------------------------------------------
# Helpers — detach objects so they are usable outside the session
# ---------------------------------------------------------------------------

def _copy(row: Ticket) -> Ticket:
    """Return a transient copy of the ORM row safe to use after session close."""
    t = Ticket()
    t.ticket_id = row.ticket_id
    t.description = row.description
    t.raised_by = row.raised_by
    t.registered_date = row.registered_date
    t.priority = row.priority
    t.status = row.status
    t.domain = row.domain
    t.resolved_by = row.resolved_by
    t.resolved_date = row.resolved_date
    return t


def _detach(session, ticket: Ticket) -> Ticket:
    session.expunge(ticket)
    return ticket

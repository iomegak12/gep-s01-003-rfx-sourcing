"""Seed script — populates sample tickets on first startup.

Seeding is skipped automatically when the database already contains tickets.
Set SEED_ON_STARTUP=false in .env to disable entirely.
"""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

from sqlalchemy import func, select

from ticket_mgmt.db.engine import SessionLocal
from ticket_mgmt.db.models import Ticket
from ticket_mgmt.enums import Priority, Status
from ticket_mgmt.repositories.ticket_repo import next_ticket_id

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _ago(days: int) -> datetime:
    """Return a UTC-aware datetime *days* before now."""
    return datetime.now(timezone.utc) - timedelta(days=days)


# ---------------------------------------------------------------------------
# Sample data  (20 tickets, Indian names, 5 domains, mixed statuses)
# ---------------------------------------------------------------------------

_SEED_TICKETS: list[dict] = [
    # ── OPEN (6) ──────────────────────────────────────────────────────────
    {
        "description": "Login page fails to load on mobile browsers after v2.3 deployment",
        "raised_by": "Arjun Sharma",
        "priority": Priority.HIGH,
        "status": Status.OPEN,
        "domain": "auth-service",
        "registered_date": _ago(5),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "RFX event notifications not delivered to invited suppliers",
        "raised_by": "Priya Patel",
        "priority": Priority.CRITICAL,
        "status": Status.OPEN,
        "domain": "event-service",
        "registered_date": _ago(3),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "Email alerts for bid deadline are sent with incorrect timestamps",
        "raised_by": "Rohit Nair",
        "priority": Priority.MEDIUM,
        "status": Status.OPEN,
        "domain": "notification-service",
        "registered_date": _ago(7),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "API gateway returns 502 intermittently under high concurrent load",
        "raised_by": "Ananya Krishnan",
        "priority": Priority.LOW,
        "status": Status.OPEN,
        "domain": "gateway",
        "registered_date": _ago(10),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "Bid scoring engine assigns zero score when supplier submits partial responses",
        "raised_by": "Vikram Mehta",
        "priority": Priority.HIGH,
        "status": Status.OPEN,
        "domain": "bid-scoring-service",
        "registered_date": _ago(2),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "JWT tokens expire prematurely — users logged out before session timeout",
        "raised_by": "Neha Gupta",
        "priority": Priority.CRITICAL,
        "status": Status.OPEN,
        "domain": "auth-service",
        "registered_date": _ago(1),
        "resolved_by": None,
        "resolved_date": None,
    },
    # ── IN_PROGRESS (4) ───────────────────────────────────────────────────
    {
        "description": "Supplier invitation emails arrive without the event attachment link",
        "raised_by": "Suresh Iyer",
        "priority": Priority.HIGH,
        "status": Status.IN_PROGRESS,
        "domain": "event-service",
        "registered_date": _ago(8),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "Automated scoring does not handle multi-currency bid submissions",
        "raised_by": "Kavitha Reddy",
        "priority": Priority.CRITICAL,
        "status": Status.IN_PROGRESS,
        "domain": "bid-scoring-service",
        "registered_date": _ago(6),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "Push notifications are not sent when a buyer updates the RFX deadline",
        "raised_by": "Manish Joshi",
        "priority": Priority.MEDIUM,
        "status": Status.IN_PROGRESS,
        "domain": "notification-service",
        "registered_date": _ago(12),
        "resolved_by": None,
        "resolved_date": None,
    },
    {
        "description": "Rate-limiting configuration blocks legitimate bulk import requests",
        "raised_by": "Deepa Agarwal",
        "priority": Priority.HIGH,
        "status": Status.IN_PROGRESS,
        "domain": "gateway",
        "registered_date": _ago(9),
        "resolved_by": None,
        "resolved_date": None,
    },
    # ── RESOLVED (5) ──────────────────────────────────────────────────────
    {
        "description": "Password reset link redirects to a blank page in Firefox",
        "raised_by": "Rajesh Kumar",
        "priority": Priority.MEDIUM,
        "status": Status.RESOLVED,
        "domain": "auth-service",
        "registered_date": _ago(15),
        "resolved_by": "Aditya Bhatt",
        "resolved_date": _ago(10),
    },
    {
        "description": "RFX close-date field not persisted when event is saved as a draft",
        "raised_by": "Sonal Desai",
        "priority": Priority.HIGH,
        "status": Status.RESOLVED,
        "domain": "event-service",
        "registered_date": _ago(18),
        "resolved_by": "Karthik Rajan",
        "resolved_date": _ago(12),
    },
    {
        "description": "Scoring rubric weights do not sum to 100 when optional criteria are removed",
        "raised_by": "Meera Pillai",
        "priority": Priority.CRITICAL,
        "status": Status.RESOLVED,
        "domain": "bid-scoring-service",
        "registered_date": _ago(20),
        "resolved_by": "Girish Naidu",
        "resolved_date": _ago(14),
    },
    {
        "description": "Digest notification emails omit line items with zero-quantity bids",
        "raised_by": "Pooja Malhotra",
        "priority": Priority.LOW,
        "status": Status.RESOLVED,
        "domain": "notification-service",
        "registered_date": _ago(22),
        "resolved_by": "Sunita Verma",
        "resolved_date": _ago(16),
    },
    {
        "description": "Gateway health-check probe returns stale upstream status for 30 seconds",
        "raised_by": "Nikhil Sinha",
        "priority": Priority.HIGH,
        "status": Status.RESOLVED,
        "domain": "gateway",
        "registered_date": _ago(17),
        "resolved_by": "Bhavna Rao",
        "resolved_date": _ago(11),
    },
    # ── CLOSED (5) ────────────────────────────────────────────────────────
    {
        "description": "Line item quantity field accepts negative values without validation",
        "raised_by": "Aditya Bhatt",
        "priority": Priority.MEDIUM,
        "status": Status.CLOSED,
        "domain": "event-service",
        "registered_date": _ago(30),
        "resolved_by": "Karthik Rajan",
        "resolved_date": _ago(25),
    },
    {
        "description": "Session tokens shared across browser tabs cause concurrent login conflicts",
        "raised_by": "Meera Pillai",
        "priority": Priority.HIGH,
        "status": Status.CLOSED,
        "domain": "auth-service",
        "registered_date": _ago(25),
        "resolved_by": "Arjun Sharma",
        "resolved_date": _ago(20),
    },
    {
        "description": "Weighted scoring algorithm ignores tie-breaker rule from evaluation template",
        "raised_by": "Girish Naidu",
        "priority": Priority.CRITICAL,
        "status": Status.CLOSED,
        "domain": "bid-scoring-service",
        "registered_date": _ago(35),
        "resolved_by": "Vikram Mehta",
        "resolved_date": _ago(28),
    },
    {
        "description": "SMS fallback not triggered when primary email delivery fails three times",
        "raised_by": "Sunita Verma",
        "priority": Priority.LOW,
        "status": Status.CLOSED,
        "domain": "notification-service",
        "registered_date": _ago(28),
        "resolved_by": "Manish Joshi",
        "resolved_date": _ago(22),
    },
    {
        "description": "CORS headers missing on preflight responses from the API gateway",
        "raised_by": "Bhavna Rao",
        "priority": Priority.MEDIUM,
        "status": Status.CLOSED,
        "domain": "gateway",
        "registered_date": _ago(32),
        "resolved_by": "Deepa Agarwal",
        "resolved_date": _ago(26),
    },
]


# ---------------------------------------------------------------------------
# Public entry-point
# ---------------------------------------------------------------------------

def seed_sample_tickets(force: bool = False) -> int:
    """Insert sample tickets if the database is empty.

    Args:
        force: When *True* insert seed data even when tickets already exist.
               Intended for development/reset workflows only.

    Returns:
        Number of tickets inserted (0 when seeding was skipped).
    """
    with SessionLocal() as session:
        existing: int = session.execute(
            select(func.count()).select_from(Ticket)
        ).scalar_one()

    if existing > 0 and not force:
        return 0

    inserted = 0
    for item in _SEED_TICKETS:
        ticket_id = next_ticket_id()
        ticket = Ticket(ticket_id=ticket_id, **item)
        with SessionLocal() as session:
            session.add(ticket)
            session.commit()
        inserted += 1

    return inserted

"""SQLAlchemy ORM models for the ticket management system."""

from __future__ import annotations

from datetime import datetime

from sqlalchemy import DateTime, Integer, String
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column


class Base(DeclarativeBase):
    pass


class Ticket(Base):
    __tablename__ = "tickets"

    ticket_id: Mapped[str] = mapped_column(String, primary_key=True)
    description: Mapped[str] = mapped_column(String, nullable=False)
    raised_by: Mapped[str] = mapped_column(String, nullable=False)
    registered_date: Mapped[datetime] = mapped_column(DateTime, nullable=False)
    priority: Mapped[str] = mapped_column(String, nullable=False, default="MEDIUM")
    status: Mapped[str] = mapped_column(String, nullable=False, default="OPEN")
    domain: Mapped[str | None] = mapped_column(String, nullable=True)
    resolved_by: Mapped[str | None] = mapped_column(String, nullable=True)
    resolved_date: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)


class IdSequence(Base):
    __tablename__ = "id_sequence"

    name: Mapped[str] = mapped_column(String, primary_key=True)
    next_value: Mapped[int] = mapped_column(Integer, nullable=False)

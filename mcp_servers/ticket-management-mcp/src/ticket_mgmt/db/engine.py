"""SQLAlchemy engine, session factory, and database initialisation."""

from __future__ import annotations

from pathlib import Path

from sqlalchemy import create_engine
from sqlalchemy.orm import Session, sessionmaker

from ticket_mgmt.config import get_settings
from ticket_mgmt.db.models import Base, IdSequence

_settings = get_settings()

# Ensure the data directory exists before creating the SQLite file.
if _settings.database_url.startswith("sqlite:///"):
    _db_path = _settings.database_url.replace("sqlite:///", "", 1)
    Path(_db_path).parent.mkdir(parents=True, exist_ok=True)

engine = create_engine(
    _settings.database_url,
    connect_args={"check_same_thread": False},  # required for SQLite + multi-thread
    echo=False,
)

SessionLocal: sessionmaker[Session] = sessionmaker(
    bind=engine,
    autocommit=False,
    autoflush=False,
)


def init_db() -> None:
    """Create all tables and seed the ticket ID counter if it does not exist."""
    Base.metadata.create_all(engine)
    with SessionLocal() as session:
        row = session.get(IdSequence, "ticket")
        if row is None:
            session.add(IdSequence(name="ticket", next_value=10001))
            session.commit()

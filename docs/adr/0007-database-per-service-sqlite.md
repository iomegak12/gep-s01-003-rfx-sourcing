# ADR 0007 — Database-Per-Service with SQLite

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §10 · D4.1, D4.2, D4.5, D4.7   |

---

## Context

The microservices architecture pattern prescribes that each service owns its data and that no service reads another service's database directly. Two decisions must be made jointly:

1. **The database engine** — what stores the data?
2. **The ownership boundary** — does each service have its own database, or is there a shared instance with separate schemas?

Constraints shaping the decision:

- **No containerisation** in this release; services run as bare local processes.
- **Zero install ceremony** — the demo must start within a minute on a clean developer machine.
- **Realistic enough** to demonstrate persistence, transactions, and migrations credibly to architects.
- **Language-neutral** — both .NET (EF Core) and Python (SQLAlchemy) must work with the chosen engine without friction.

## Decision

**SQLite, file-based, with one independent database file per service.**

| Service             | Database File         | ORM                          |
| ------------------- | --------------------- | ---------------------------- |
| Event Service       | `event_service.db`    | EF Core 8                    |
| Bid Scoring Service | `bid_scoring.db`      | SQLAlchemy 2.x (sync)        |

Both files live in the working directory of their respective services, are gitignored, and are recreated on each clean run (`./scripts/clean.ps1`). Schema is managed by EF Core Migrations (.NET) and Alembic (Python).

**No service reads another service's SQLite file.** All cross-service data exchange occurs through REST APIs.

## Alternatives Considered

| Alternative                                  | Reason for Rejection                                                                                       |
| -------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| PostgreSQL local instance                    | Requires installation/setup; eats setup time; offers no architectural insight beyond what SQLite provides for this scope. |
| Single shared database with separate schemas | Anti-pattern in microservices; couples services through the database tier; defeats the architectural narrative. |
| In-memory SQLite                             | Loses data on restart; cannot demonstrate persistence credibly.                                            |
| File-based JSON / NoSQL local store          | Cannot demonstrate ORMs, migrations, or transactions in the way architects expect.                         |

## Consequences

### Positive

- **Zero installation friction**; SQLite is bundled with both runtimes.
- **Authentic database-per-service pattern** — each service has its own physical file; the boundary is enforceable, not aspirational.
- **Migrations work realistically** — EF Core Migrations and Alembic both produce real migration files that can be inspected, version-controlled, and demonstrated.
- **Deterministic demo** — `./scripts/clean.ps1` returns the system to a known state instantly.
- **Adequate for demo data volumes** (tens of events, hundreds of bids).

### Negative

- **Single-writer limitation** of SQLite is irrelevant for the demo workload but would surface immediately under any concurrent write load.
- **Limited query optimisation** compared to PostgreSQL; advanced indexing demonstrations are constrained.
- **Production discontinuity** — no production system would use SQLite; the migration to PostgreSQL/SQL Server/etc. for higher environments is a separate exercise (mitigated because EF Core and SQLAlchemy abstract the engine).

### Neutral

- The decision is fully reversible. EF Core and SQLAlchemy both support SQLite, PostgreSQL, SQL Server, and other engines via configuration changes; switching engines for a higher environment requires updating the connection string and re-applying migrations.

# Architecture Decision Records (ADRs)

This folder contains the Architecture Decision Records for the **RFx Sourcing Event Management** capability. Each ADR captures one significant architectural decision in the lightweight Michael Nygard format — *Context → Decision → Consequences* — with light extensions for status, date, and alternatives considered.

ADRs are intended to be **short, immutable, and consequential**. They explain *why* the architecture looks the way it does to anyone joining the team in three months, six months, or three years.

---

## Index

| ADR # | Title                                                                                       | Status                |
| ----- | ------------------------------------------------------------------------------------------- | --------------------- |
| 0001  | [Adopt Layered Architecture Within Each Service](./0001-layered-architecture.md)            | Accepted              |
| 0002  | [Use Synchronous REST for Inter-Service Communication](./0002-sync-rest.md)                 | Accepted              |
| 0003  | [Spec-Driven Development with OpenAPI as Source of Truth](./0003-spec-driven-development.md)| Accepted              |
| 0004  | [URL Path Versioning for APIs](./0004-url-path-versioning.md)                               | Accepted              |
| 0005  | [RFC 7807 Problem Details for Error Responses](./0005-problem-details.md)                   | Accepted              |
| 0006  | [JWT Validation at Gateway Only (Demo Simplification)](./0006-jwt-validation-at-gateway.md) | Accepted (with limitation) |
| 0007  | [Database-Per-Service with SQLite](./0007-database-per-service-sqlite.md)                   | Accepted              |
| 0008  | [Mono-Repo Strategy for Greenfield Microservices](./0008-monorepo-strategy.md)              | Accepted              |
| 0009  | [PowerShell Core for Cross-Platform Run Scripts](./0009-powershell-core-scripts.md)         | Accepted              |
| 0010  | [Defer OpenTelemetry to Phase 2; Correlation ID as Bridge](./0010-defer-opentelemetry.md)   | Accepted (with evolution path) |

---

## Conventions

- **Numbering** — ADRs are numbered sequentially from 0001. Numbers are never reused, even if an ADR is later superseded.
- **Status values** — `Proposed`, `Accepted`, `Deprecated`, `Superseded by ADR XXXX`.
- **Filename format** — `NNNN-kebab-case-title.md`.
- **Immutability** — ADRs are not edited after acceptance. To revise a decision, write a new ADR that supersedes the original and update the original's status to `Superseded by ADR XXXX`.

---

## Format

Each ADR follows the structure:

```
# ADR NNNN — Title

| Field          | Value          |
| -------------- | -------------- |
| Status         | ...            |
| Date           | ...            |
| Decision Owner | ...            |
| Related        | HLD section, decision IDs, risk IDs |

## Context
   The forces in play; what made this decision necessary.

## Decision
   The decision itself, stated clearly.

## Alternatives Considered
   What else was on the table and why it was not chosen.

## Consequences
   ### Positive
   ### Negative
   ### Neutral
```

---

## Related Documents

- [`../HLD.md`](../HLD.md) — High-Level Design (full architectural narrative).
- [`../BRD.md`](../BRD.md) — Business Requirements Document.

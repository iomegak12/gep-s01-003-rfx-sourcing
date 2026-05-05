# ADR 0001 — Adopt Layered Architecture Within Each Service

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §5.1, §5.3 · D1.1              |

---

## Context

The RFx Sourcing Event capability is delivered as two backend microservices — a .NET 8 / ASP.NET Core Event Service and a Python 3.11 / FastAPI Bid Scoring Service. We need to choose an internal architecture pattern for each service that balances:

- **Demonstrability** — the audience (experienced designers, engineers, solution architects) must be able to follow the structure without prior context, within a 12-hour build window.
- **Symmetry across stacks** — the same mental model should apply to both services so participants do not have to relearn navigation when flipping between .NET and Python.
- **Industry recognisability** — architects in the room should immediately recognise the pattern as production-grade, not toy code.
- **Future evolvability** — the structure must accommodate growth (additional features, additional services) without significant refactoring.

Several mature alternatives were considered, ranging from minimal layering to full Domain-Driven Design.

## Decision

**Both services adopt a four-layer (N-tier) architecture**: Controller/Router → Service → Repository → Domain (with cross-cutting concerns applied across all layers).

Concretely:

- **.NET (Event Service):** `Controllers/`, `Services/`, `Repositories/`, `Domain/`, `Infrastructure/` folders within a single project.
- **Python (Bid Scoring Service):** `app/routers/`, `app/services/`, `app/repositories/`, `app/domain/`, `app/infrastructure/` modules within a single package.

## Alternatives Considered

| Alternative                          | Reason for Rejection                                                                                                |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| Clean Architecture (multi-project)   | Adds 3–4 extra projects per service. Plumbing dominates the demo time without proportional teaching value.          |
| Hexagonal / Ports-and-Adapters       | Excellent for testability and decoupling, but the abstraction overhead distracts from the AI-assisted workflow focus.|
| DDD with Aggregates and Domain Events| Overkill for a CRUD-with-workflow feature; aggregate boundaries become academic for the in-scope FRs.               |
| Vertical Slice / Feature Folders     | Modern and increasingly preferred, but mixes uneasily with a layered narrative; chose layered for symmetry across stacks.|

## Consequences

### Positive

- Familiar to virtually every backend engineer; near-zero ramp-up.
- Symmetric across both stacks — participants can navigate either service confidently.
- Each layer has a clear, single responsibility; testing strategy follows naturally (unit-test the Service layer, integration-test through the Controller).
- Sufficient structure to grow into additional features without immediate refactoring pressure.

### Negative

- Less expressive about domain invariants than DDD; complex domain rules (when they emerge) will need conscious encapsulation in the Service layer to avoid leaking into Controllers or Repositories.
- Repositories can become a "junk drawer" if the Service layer is thin; discipline is required to keep persistence concerns out of business logic.
- The pattern offers fewer guardrails than Clean Architecture; teams must self-enforce dependency direction (Controllers → Services → Repositories, never the reverse).

### Neutral

- This decision is reversible: the structure can be evolved toward Clean Architecture or Vertical Slices in a future release without a full rewrite.

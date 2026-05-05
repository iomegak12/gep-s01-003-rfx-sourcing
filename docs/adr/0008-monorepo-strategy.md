# ADR 0008 — Mono-Repo Strategy for Greenfield Microservices

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §12.1, §12.2 · D6.1, D6.2      |

---

## Context

Two microservices, an API gateway, OpenAPI specifications, design documentation, ADRs, and developer scripts must be organised. The repository strategy must be chosen between:

- **Mono-repo** — one Git repository contains everything.
- **Poly-repo** — one repository per deployable component (gateway, Event Service, Bid Scoring Service).
- **Hybrid** — mono-repo with Git submodules per service.

The decision is consequential for the AI-assisted development workflow being demonstrated: the AI agent (Claude Code) operates on the codebase available in its working tree, and a single mono-repo gives the agent visibility into the entire system context without cross-repository hops.

## Decision

**A single mono-repo holds the gateway, both services, contracts, documentation, and scripts.**

```
rfx-sourcing/
├── docs/                           ← BRD, HLD, ADRs
├── contracts/                      ← OpenAPI specs (versioned), Postman collection
├── gateway/                        ← .NET YARP gateway
├── services/
│   ├── event-service/              ← .NET Event Service
│   └── bid-scoring-service/        ← Python Bid Scoring Service
└── scripts/                        ← PowerShell Core scripts
```

## Alternatives Considered

| Alternative                                          | Reason for Rejection in This Release                                                              |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| Poly-repo (one repo per service + one for gateway)   | Three clones to bootstrap the demo; scattered documentation; AI agent loses cross-system context. |
| Two repos (gateway folded into one service repo)     | Asymmetric; one service is "more equal" than the other.                                           |
| Mono-repo with Git submodules per service            | Adds submodule complexity for negligible benefit at this scale.                                   |

## Consequences

### Positive

- **One `git clone` bootstraps the entire demo environment.**
- **Centralised documentation and contracts** — `/docs` and `/contracts` are first-class citizens, not duplicated or scattered.
- **Atomic cross-cutting changes** — a contract change in `/contracts/event-service/v1/openapi.yaml` and the corresponding controller change in `/services/event-service/Controllers/` ship in the same commit.
- **AI-assisted workflow leverage** — Claude Code sees the full repository, enabling cross-service reasoning, refactors, and consistency checks that would be impossible across separate repos.
- **Simpler CI** when CI is added in the future.

### Negative

- **Coupled release cadence** by default — with a mono-repo, "ship one service" requires path-based CI filters or equivalent discipline. For a true production microservices system at scale, this trade-off is real and may favour poly-repo.
- **Larger clone size** as the project grows; trivial at this scale, meaningful at hundreds of services.
- **Access control granularity** is coarser — the entire repo is governed as one unit. Acceptable in a single-team context; problematic in a multi-team enterprise.

### Neutral

- The decision is reversible by splitting the mono-repo into multiple repos using `git filter-repo` or similar tooling when team scale or release cadence justifies it. The internal folder structure (`/services/<service>/...`) is poly-repo-friendly and would split cleanly.

## Production Note

Real microservices at GEP scale typically prefer poly-repo (or trunk-based mono-repos with sophisticated CI) for autonomy and independent release cadence. This ADR captures the decision **for this demo and its training context** and explicitly does not prescribe the strategy for production GEP systems.

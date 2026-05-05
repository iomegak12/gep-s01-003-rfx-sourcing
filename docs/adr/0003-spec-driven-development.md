# ADR 0003 — Spec-Driven Development with OpenAPI as Source of Truth

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §8.2, §15.2 · D2.2, D6.5       |

---

## Context

Each microservice exposes a REST API that is consumed by the API Gateway, by Service A (for east-west calls to Service B), and by external clients (Postman, future frontends). The team must decide the **source of truth** for these contracts: the OpenAPI specification, or the implementation code.

Two genuinely defensible philosophies exist:

- **Code-first** — write controllers/routers, let the framework auto-generate the OpenAPI document on startup.
- **Spec-first (a.k.a. contract-first)** — hand-author the OpenAPI document before implementation, then align code to it.

The decision is consequential because the AI-assisted development workflow (using Claude Code) treats specifications as high-fidelity inputs. A well-formed OpenAPI document is one of the most valuable artefacts an AI agent can consume to generate aligned code, tests, and contract validators.

## Decision

**OpenAPI 3.x is the source of truth.** Specifications are hand-authored **before** implementation and live in version-controlled folders, decoupled from any service's source code:

```
/contracts/event-service/v1/openapi.yaml
/contracts/bid-scoring-service/v1/openapi.yaml
```

Service code is then aligned to the spec. Drift between spec and implementation is treated as a defect in the implementation, not a permitted divergence.

## Alternatives Considered

| Alternative                                | Reason for Rejection                                                                                            |
| ------------------------------------------ | --------------------------------------------------------------------------------------------------------------- |
| Code-first (auto-generate spec)            | Reverses the AI-assisted workflow: the spec becomes a consequence of code rather than a driver. Loses leverage. |
| No formal contract; markdown documentation | Cannot drive code generation, contract testing, or client tooling. Drifts immediately in practice.              |
| Contract-first using a managed tool (Stoplight, Spectral as primary) | Tooling overhead disproportionate to a demo build; raw OpenAPI YAML is sufficient.    |

## Consequences

### Positive

- The AI agent (Claude Code) receives a precise, machine-readable contract as input, materially improving code generation quality and reducing review burden.
- Contracts are reviewable independently of implementation, enabling parallel work (one engineer drafts spec, another scaffolds code).
- Postman collections, mock servers, and contract tests can all be derived from the spec.
- API evolution becomes visible at the filesystem level (versioned folders).

### Negative

- Hand-authoring OpenAPI YAML has a learning curve; teams unfamiliar with it spend the first hour battling syntax rather than designing the API.
- Spec/code drift is a real risk if discipline lapses; mitigations include contract tests in CI (future) and code review checks.
- Auto-generated specs from frameworks (e.g., Swashbuckle, FastAPI's automatic OpenAPI) are still produced at runtime — these are *not* the source of truth and may diverge from the hand-authored spec. The hand-authored spec wins.

### Neutral

- This decision can be revisited per-service if one service's API surface becomes too dynamic for hand-authored contracts (rare in practice).

# LLD Deliverable Prompt — RFx Sourcing Event Management

## Context

The technical architecture team has approved the BRD ([BRD.md](BRD.md)) and HLD ([HLD.md](HLD.md)). The HLD explicitly defers concrete endpoint shapes, payload schemas, database DDL, algorithmic detail, Polly thresholds, claim names, and lifecycle edge cases to the LLD. The next required artefact is therefore a pair of build-ready Low-Level Design documents — one per microservice — that turns those deferred items into specifications a developer can implement without further interpretation.

This document is the **working brief** for that LLD work: it captures every decision locked through clarification with the facilitator and the structure both LLDs must follow.

**Hard constraint:** the LLDs must contain **no source code, scripts, HTML, CSS, or JavaScript**. They are specification documents only. Permitted forms: prose, markdown tables, mermaid diagrams, JSON request/response examples (as OpenAPI-style spec data), and configuration-key tables.

---

## Locked Decisions

### Domain & schema

| # | Decision |
|---|----------|
| D1 | **Bid granularity:** event-level — one bid per supplier per event. |
| D2 | **Bid attribute model:** criterion-driven — `bid_attributes` table holds `{criterion_id → value}` pairs. Whatever criteria the buyer defines become the fields the bid must populate. |
| D3 | **Scoring algorithm:** min-max normalize each criterion's values across all bids (with direction-aware inversion for lower-is-better criteria), then weighted sum. |
| D4 | **Award rationale:** single free-text `rationale` field, max 1000 characters. |
| D5 | **Cancelled state:** out of scope — documented in state machine but not implemented. |
| D6 | **Bid revisions:** one-shot only. No update endpoint, no revisions table. |
| D7 | **ID strategy:** UUID v7, stored as TEXT in SQLite, used for all primary keys and cross-service references. |

### Cross-service behaviour

| # | Decision |
|---|----------|
| D8 | **Publish gates:** strict — Event Service requires ≥1 line item, ≥1 supplier (local checks) and calls Bid Scoring east-west to confirm ≥1 criterion exists for the event before transitioning to Published. |
| D9 | **Bid acceptance gate:** Bid Scoring calls Event Service east-west on every bid POST to confirm the event is in Published or Bidding state. |
| D10 | **Award flow:** unchanged from HLD §7.3 (Event Service calls Bid Scoring for ranked bids, validates winning supplier is in the ranked list, persists award, transitions event to Awarded). |

### Cross-cutting

| # | Decision |
|---|----------|
| D11 | **Audit trail:** structured logs **and** a dedicated `audit_events` table per service. Both services write the same lifecycle event names. |
| D12 | **Polly thresholds:** sensible defaults proposed in the LLD, **fully adjustable via configuration**. |
| D13 | **Roles in JWT:** only `Buyer`. Award endpoint role-gated; all other endpoints authenticated-only. |
| D14 | **Health endpoints:** basic liveness only — `200 OK` with `{status:"ok"}`. One endpoint per process. |
| D15 | **Pagination:** offset/limit on all list endpoints (`?limit=50&offset=0`); default `limit=50`, max `limit=200`. |

### Output format

| # | Decision |
|---|----------|
| D16 | **File paths:** [LLD-event-service.md](LLD-event-service.md) and [LLD-bid-scoring-service.md](LLD-bid-scoring-service.md). |
| D17 | **Style:** mirror the HLD — numbered sections, mermaid diagrams, tables. |
| D18 | **OpenAPI representation:** endpoint summary table + per-endpoint request/response JSON example + field-level table. **No full YAML or JSON OpenAPI document.** |
| D19 | **Sequence diagrams:** Publish (east-west criteria check), Submit Bid (east-west event-status check), Score Bids (internal compute), Award (re-included with LLD-level guards and audit-events writes). |

---

## Common LLD Section Outline

| § | Section | Mandatory requirement covered |
|---|---------|-------------------------------|
| 1 | Document Purpose & Scope | — |
| 2 | Project Structure | ✅ Project structure |
| 3 | Modular Structure | ✅ Modular structure |
| 4 | Database Schema | ✅ Database schema |
| 5 | REST Endpoints (summary) | ✅ REST endpoints |
| 6 | Request & Response Structures | ✅ Request/response structure |
| 7 | OpenAPI 3.1 Documentation Surface | ✅ OpenAPI / Swagger |
| 8 | Error Model — RFC 7807 | ✅ RFC error structure |
| 9 | Cross-Cutting Concerns | ✅ Cross-cutting concerns |
| 10 | Authentication & Authorisation | — |
| 11 | Resilience — Retry / CB / Timeout / Bulkhead | ✅ Resilience patterns |
| 12 | Health Endpoint Contract | ✅ Health endpoints |
| 13 | Sequence Diagrams | ✅ Sequence diagrams |
| 14 | External Dependencies / Packages | ✅ Packages & external dependencies |
| 15 | Seed Data Specification | — |
| 16 | Feature Traceability | — |
| 17 | Configuration Reference | — |
| 18 | Open Items / Deferred to Implementation | — |

---

## Resilience defaults (configurable per D12)

| Pattern | Default | Configurable via |
|---------|---------|------------------|
| Per-attempt timeout | 5 s | `Resilience:Timeout:PerAttemptSeconds` (.NET) / `RESILIENCE_TIMEOUT_PER_ATTEMPT_SEC` (Python) |
| Retry — count | 3 | `Resilience:Retry:Count` / `RESILIENCE_RETRY_COUNT` |
| Retry — backoff | Exponential, base 200 ms, max 2 s, with jitter | `Resilience:Retry:BackoffBaseMs`, `MaxBackoffMs`, `JitterEnabled` |
| Retry — retryable status | 408, 429, 500, 502, 503, 504 | `Resilience:Retry:RetryableStatuses` |
| Circuit breaker — failure ratio | 0.5 over 30 s | `Resilience:Breaker:FailureRatio`, `SamplingWindowSec` |
| Circuit breaker — minimum throughput | 10 calls in window | `Resilience:Breaker:MinimumThroughput` |
| Circuit breaker — break duration | 10 s | `Resilience:Breaker:BreakDurationSec` |
| Bulkhead — max concurrent | 10 | `Resilience:Bulkhead:MaxConcurrent` |
| Bulkhead — max queued | 20 | `Resilience:Bulkhead:MaxQueued` |

Composition order: **Bulkhead → Retry → Circuit Breaker → Timeout → HTTP call.**

---

## RFC 7807 error model

Common Problem Details body shape, status-code table, and a per-service exception → status mapping. Status mapping covers: 400 (validation), 401 (gateway only), 403 (role gate), 404 (not found), 409 (state machine / uniqueness), 422 (semantic validation), 502/503/504 (east-west failures).

`type` URIs follow `https://rfx-sourcing/problems/<slug>`. Both services use the same slug taxonomy.

---

## Verification checklist

1. Every FR-01..FR-12 traced to at least one endpoint, table, and sequence step across the two LLDs.
2. All mermaid diagrams render in a markdown previewer.
3. Endpoint count in §5 equals the per-endpoint detail count in §6.
4. Every threshold/value referenced in §11/§9/§10 has a matching key in §17.
5. No fenced code blocks tagged `csharp`, `cs`, `python`, `py`, `sql`, `powershell`, `ps1`, `bash`, `sh`, `html`, `css`, `javascript`, `js`, `typescript`, `ts`, `xml`, `yaml`, `yml`. Only `mermaid` and `json` allowed.
6. Style and section numbering visually match [HLD.md](HLD.md).

---

## Files to be produced

- [LLD-event-service.md](LLD-event-service.md)
- [LLD-bid-scoring-service.md](LLD-bid-scoring-service.md)

---

*End of Document*

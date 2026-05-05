# ADR 0010 — Defer OpenTelemetry to Phase 2; Correlation ID as Bridge

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted (with documented evolution path) |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §11.2, §15.4 · D2.7, D5.7, D9.5, R5 |

---

## Context

Mature distributed systems are observable along three axes: **logs** (what happened), **metrics** (how often, how fast), and **traces** (the path a request took across services). The industry-standard solution for unified observability is **OpenTelemetry (OTel)** — a CNCF project providing SDKs in both .NET and Python, with W3C Trace Context (`traceparent` header) as the propagation standard.

The question for this release: introduce OTel now, or defer?

Constraints:

- **No containerisation in this release** — running an OTel Collector, a Jaeger backend, and a Prometheus instance bare-metal is operationally heavy.
- **12-hour build window** — every architectural component takes time; observability infrastructure that the demo will not visibly use carries a poor return on time.
- **Audience expectations** — architects will absolutely ask "how do you observe this in production?" and the answer must be coherent.

## Decision

**OpenTelemetry is deferred to Phase 2.** This release ships with:

- **Structured JSON logs** (Serilog in .NET, structlog in Python) emitted to stdout (per service, per process).
- A **`X-Correlation-Id`** header generated at the gateway and propagated across all hops; included in every log entry.
- **`/health` endpoints** on each service for liveness checks.

The `X-Correlation-Id` mechanism is intentionally chosen as the **bridge** to a future OTel rollout: it provides today's traceability and tomorrow's migration target.

## Alternatives Considered

| Alternative                                          | Reason for Rejection in This Release                                                                |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| Full OTel stack (SDK + Collector + Jaeger backend)   | Significant infrastructure overhead incompatible with the no-container constraint.                  |
| OTel SDK wired but exporting to console only         | Code present, no visible value during demo; risks looking like dead weight.                         |
| Prometheus metrics endpoint per service              | Useful in production; without a Prometheus scraper running locally, the endpoint serves no demo purpose. |
| Vendor APM (Datadog, New Relic)                      | Cloud dependency; out of scope for local demo; vendor-locked.                                       |

## Consequences

### Positive

- **Build time preserved** for higher-leverage architectural concerns.
- **Correlation IDs visible immediately** — three terminal windows during the demo show the same correlation ID flowing through gateway, Service A, and Service B logs. This is *traceability the audience can see*, even without OTel.
- **Honest, simple narrative**: "today we have logs and correlation; here is the documented path to full distributed tracing."
- **No code waste** — nothing built today is thrown away when OTel arrives.

### Negative — and openly documented (Risk R5)

- **No metrics** — request rates, error rates, latency percentiles must be derived from logs.
- **No span-level tracing** — root-cause analysis on slow requests requires correlating logs by hand.
- **Acceptable for a demo, not for production.**

### Neutral

- The decision is reversible at moderate cost (Phase 2 work).

## Phase 2 Evolution Path

When OpenTelemetry is introduced, the migration is non-disruptive:

1. **Add the OTel SDK** to both services (`OpenTelemetry.Extensions.Hosting` for .NET, `opentelemetry-distro` for Python).
2. **Replace the gateway's custom `X-Correlation-Id` generation with W3C `traceparent` header** generation. The header carries the trace ID, span ID, and trace flags in a standard format.
3. **Configure exporters** to ship spans to an OTel Collector, which forwards to a trace backend (Jaeger, Tempo, Honeycomb, etc.).
4. **Configure metrics exporters** to ship to Prometheus or an equivalent.
5. **Update structured logging** to enrich each log entry with the OTel trace ID and span ID (instead of the legacy correlation ID), enabling correlation between logs and traces in the backend.

Crucially, **application code does not change**. The instrumentation is auto-injected at the framework level for ASP.NET Core controllers, EF Core queries, HTTP outbound calls, FastAPI routes, and SQLAlchemy queries.

The HLD §15.4 diagram illustrates this evolution.

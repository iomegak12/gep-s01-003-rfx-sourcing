# ADR 0002 — Use Synchronous REST for Inter-Service Communication

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §8 · D2.1, D2.3, D1.2          |

---

## Context

The Event Service and Bid Scoring Service must exchange data. Specifically, the Award action in the Event Service requires a ranked list of bids that lives in the Bid Scoring Service. The communication style for this and any future east-west traffic must be selected.

Constraints shaping the decision:

- **Local-only execution** with no containerisation in this release; running message brokers (Kafka, RabbitMQ) on bare metal is operationally heavy for a 12-hour demo.
- **Mixed stacks (.NET and Python)** with no common RPC framework already in place.
- **Demonstrability** — the chosen mechanism must be observable to a live audience using familiar tools (Postman, browser DevTools, terminal logs).
- **Architect credibility** — the choice must be defensible to senior engineers, not merely pragmatic.

## Decision

**All inter-service communication is synchronous REST over HTTP/1.1 with JSON payloads.** No asynchronous messaging, no gRPC, no GraphQL.

Service A (Event Service) consumes Service B (Bid Scoring Service) via a typed HTTP client registered through `IHttpClientFactory`, with Polly resilience policies configured (Retry, Circuit Breaker, Timeout, Bulkhead).

## Alternatives Considered

| Alternative                          | Reason for Rejection                                                                                                |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| gRPC with Protobuf                   | Faster on the wire and strongly typed, but tooling overhead in two stacks (codegen, build integration) eats demo time. Less observable in Postman. |
| GraphQL federation                   | Excellent for client-driven queries; adds complexity and a query layer that does not pay off in a 2-service system. |
| Asynchronous messaging (Kafka/RabbitMQ) | The "right" answer for some workflows (Saga, Outbox) but requires broker infrastructure incompatible with the no-container constraint. |
| Mixed protocols (REST north-south, gRPC east-west) | Two protocols to teach in 12 hours; cognitive overhead not justified.                                |

## Consequences

### Positive

- Lingua franca: every architect in the room understands REST/JSON without preamble.
- Easily demonstrable: requests visible in Postman, responses readable in logs, contract documented in OpenAPI.
- Toolchain-light: no protobuf compilation, no broker setup, no consumer group management.
- Forces an honest conversation about resilience patterns (Polly), which the audience can see in code.

### Negative

- **Tight runtime coupling**: an outage in Service B blocks the Award action in Service A. Polly mitigates short transient failures; sustained outages propagate. Documented as Risk R7 in the HLD.
- **No native eventual consistency story**: cross-service writes have at-most local-transaction guarantees. Documented as Risk R2.
- **Latency floor**: each east-west call adds an HTTP round-trip; for chatty workflows this would compound. Acceptable here because the Award action makes one east-west call.
- **No event-driven extensibility** (e.g., other services subscribing to "event awarded" notifications) without a future architectural evolution to introduce messaging.

### Neutral

- The decision is partially reversible: introducing asynchronous messaging later does not require removing REST; the two coexist in mature systems. The Outbox pattern is the natural bridge.

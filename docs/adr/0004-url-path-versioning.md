# ADR 0004 — URL Path Versioning for APIs

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §8.5, §15.1 · D2.5, D9.1       |

---

## Context

REST APIs evolve. Breaking changes are sometimes unavoidable. A versioning strategy must be selected upfront so that breaking changes can be introduced without disrupting existing consumers.

The major options are URL path versioning (`/api/v1/events`), header-based versioning (`Accept: application/vnd.gep.v1+json`), query parameter versioning (`/api/events?version=1`), or no versioning until forced.

Considerations:

- **Visibility** — versioning must be obvious to developers reading logs, browsing Postman, or tracing requests.
- **Routing simplicity** — the gateway must route requests to the correct service version without complex header inspection.
- **Demonstrability** — the chosen scheme must be self-evident to a live audience.

## Decision

**URL path versioning is adopted from day one.** Every endpoint is namespaced under `/api/v{n}/...`:

- `/api/v1/events`
- `/api/v1/events/{id}/award`
- `/api/v1/bids`

Future major versions (`/api/v2/...`) will run in parallel with their predecessors during deprecation windows.

## Alternatives Considered

| Alternative                                  | Reason for Rejection                                                                                       |
| -------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Header-based (`Accept: vnd.gep.v1+json`)     | More "RESTful" academically but invisible in Postman screenshots, harder to test ad hoc, harder to log.     |
| Query parameter (`?version=1`)               | Couples versioning with query semantics; awkward for caching and routing.                                  |
| No versioning until forced                   | Defers a decision that costs almost nothing now and is expensive to retrofit. Architects expect versioning to be present. |

## Consequences

### Positive

- Immediately visible in URLs, logs, Postman, and OpenAPI specs.
- Trivial to route at the gateway: path prefix maps to backend service.
- Aligns naturally with the versioned `/contracts/<service>/v{n}/openapi.yaml` folder structure (ADR 0003).
- Side-by-side major versions are straightforward (`v1` and `v2` controllers/routers coexist).

### Negative

- Some REST purists view URL versioning as a violation of resource identity (the *same* resource at `/api/v1/events/123` and `/api/v2/events/123`). This is an academic concern in practice.
- Three concurrent major versions become unwieldy; deprecation policy must be enforced.

### Neutral

- The choice does not preclude adding header-based content negotiation for **non-breaking** representational variants (e.g., `Accept: application/json` vs `application/xml`) within the same URL version.

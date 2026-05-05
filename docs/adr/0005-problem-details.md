# ADR 0005 — RFC 7807 Problem Details for Error Responses

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §8.1, §11.3 · D2.6, D5.4       |

---

## Context

The system spans two stacks (.NET, Python) and three components (gateway, Event Service, Bid Scoring Service). Without a consistent error response format, each component would naturally drift toward its framework's defaults — ASP.NET Core's `ProblemDetails`, FastAPI's `{"detail": "..."}`, YARP's plain HTTP status codes — leading to a fragmented client experience.

A standard format is needed that:

- Is **interoperable** across both stacks with minimal effort.
- Is **machine-readable** for clients and integration tests.
- Is **human-readable** for developers debugging from logs or Postman.
- Is **recognised** by reviewing architects as a mature choice.

## Decision

**All error responses across the gateway and both services conform to RFC 7807 Problem Details** with the media type `application/problem+json`.

A typical error response shape:

```json
{
  "type": "https://example.com/probs/event-not-found",
  "title": "Event not found",
  "status": 404,
  "detail": "No event exists with id e3a1...",
  "instance": "/api/v1/events/e3a1.../",
  "correlationId": "abc-123"
}
```

In .NET, this is realised through ASP.NET Core's built-in `ProblemDetails` plus a global exception middleware. In Python (FastAPI), it is realised through a small custom exception handler that constructs the equivalent payload.

A custom exception hierarchy in each service maps to specific HTTP status codes:

- `NotFoundException` → 404
- `ValidationException` → 400
- `DomainException` → 409 (state-conflict scenarios such as "cannot publish an event with no line items")

## Alternatives Considered

| Alternative                                                  | Reason for Rejection                                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------- |
| Custom JSON envelope (`{"error": {"code": "...", "message": "..."}}`) | Reinvents the wheel; loses interoperability with tools that already understand RFC 7807. |
| Plain HTTP status code + plain text body                     | Insufficient for clients that need structured error info; poor for automated handling. |
| Each service defines its own error format                    | The fragmentation problem this ADR exists to solve.                                    |

## Consequences

### Positive

- Consistent error contract across both stacks; clients write one error handler.
- Native support in ASP.NET Core minimises implementation cost in the .NET service.
- The `correlationId` field bridges errors and logs — a developer reading an error response can trace the full request path through structured logs.
- Strong signal to reviewing architects that error handling has been considered as a first-class concern.

### Negative

- Adds a small amount of plumbing code in the FastAPI service (custom exception handler + Pydantic schema for the response).
- The `type` URI convention requires discipline; if URIs are inconsistent across services, the RFC 7807 benefit is partially eroded.
- Demanding a custom exception hierarchy adds ~15 minutes of design time per service; teams must resist the urge to throw `Exception` or `RuntimeError` directly.

### Neutral

- This decision composes well with future additions such as field-level validation errors using the RFC 7807 `errors` extension.

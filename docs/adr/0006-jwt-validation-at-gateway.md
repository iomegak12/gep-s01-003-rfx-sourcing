# ADR 0006 — JWT Validation at Gateway Only (Demo Simplification)

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted (with documented limitation) |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §9.2 · D3.2, R1                |

---

## Context

Authentication is performed against a pre-built Auth Service that issues HS256-signed JWTs. The system must decide **where** these tokens are validated:

- At the **gateway only** — services trust the gateway and skip re-validation.
- At the **gateway and at each service** — defence in depth, zero-trust.
- At each **service only** — the gateway is a dumb router.
- At the gateway with a **trusted forwarded user-context header** — the gateway validates, strips the JWT, and forwards trusted internal headers (e.g., `X-User-Id`).

In a containerised or service-mesh environment, services are typically isolated from direct external access, making gateway-only validation reasonable. In this release, however, services run as **bare local processes on fixed ports** with no network isolation — they are reachable directly on `localhost:5001` and `localhost:5002`.

## Decision

**JWT validation is performed at the API Gateway only.** Downstream services trust the gateway and do not re-validate the token. The Award endpoint additionally enforces a role check (`Buyer`) using the `roles` claim, demonstrating role-based authorisation as a working pattern.

This is an **explicit demo simplification**, accepted with a documented mitigation path (see Risk R1 in the HLD).

## Alternatives Considered

| Alternative                                          | Reason for Rejection in This Release                                                          |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Gateway + per-service re-validation (defence in depth) | Requires JWT validation libraries and shared secret distribution to both .NET and Python services. ~30 minutes of additional setup. **Recommended for production.** |
| Service-only validation                              | Reduces the gateway to a dumb router; loses centralised auth observability.                   |
| Trusted forwarded headers (`X-User-Id` after gateway strips JWT) | Industry-grade pattern (Netflix and others) but adds custom middleware in two stacks. Worth considering for a follow-up release. |

## Consequences

### Positive

- Single validation path; faster to implement and demonstrate.
- Centralised authentication policy: changes (e.g., new validation rules, additional issuer trust) apply at one location.
- Clear teaching narrative: "this is the gateway's job; services focus on business logic."

### Negative — and openly documented

- **Services accessed directly on their internal ports are unauthenticated.** Anyone with network access to `localhost:5001` or `localhost:5002` bypasses authentication entirely. This is acceptable for a single-developer-machine demo and **must not** be carried into production without a mitigation.
- The `roles` claim is consumed at the gateway-trusted boundary, which means a compromised gateway exposes everything. Production should add per-service re-validation as a safety net.
- The pattern teaches a simplification that, if internalised uncritically, leads to insecure production deployments. The HLD's Risks Register surfaces this explicitly (R1).

### Neutral

- This decision is reversible at low cost: per-service JWT validation can be added later by registering the same authentication middleware in each service. Existing application code does not change.

## Production Mitigation Path

When this design moves toward production, **at least one** of the following must be adopted:

1. **Per-service JWT re-validation** — defence in depth; each service validates the token independently.
2. **Network isolation** — services bound to private interfaces only; only the gateway is publicly addressable. Often achieved via service mesh (e.g., Istio with mTLS) or private subnets.
3. **Trusted-forwarded-headers pattern** — gateway strips the JWT and signs internal headers (e.g., HMAC-signed `X-User-Context`); services verify the signature.

The HLD §15 evolution narrative and Risk R1 capture this commitment.

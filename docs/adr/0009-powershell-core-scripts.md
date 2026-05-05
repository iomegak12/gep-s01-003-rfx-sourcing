# ADR 0009 — PowerShell Core for Cross-Platform Run Scripts

| Field          | Value                              |
| -------------- | ---------------------------------- |
| Status         | Accepted                           |
| Date           | 04 May 2026                        |
| Decision Owner | Ramkumar (Solution Architect)      |
| Related        | HLD §13.3 · D7.2                   |

---

## Context

The repository ships convenience scripts that participants and the facilitator use to run the demo: starting individual services, seeding data, cleaning state, and running smoke checks. The scripting language must be chosen with the audience in mind.

Considerations:

- **Audience operating systems are mixed** — Indian enterprise training cohorts typically include a mix of Windows, macOS, and Linux laptops.
- **Stack alignment** — the project includes a .NET service; using a Microsoft-aligned scripting language reinforces ecosystem coherence.
- **Script quality** — the language should support typed parameters, structured output, and proper error handling, not just text-stream pipelining.
- **Cross-platform parity** — the same script must run identically on Windows, macOS, and Linux without forking.

## Decision

**All convenience scripts are written in PowerShell Core (`.ps1`) targeting PowerShell 7.0+.**

The script set:

| Script                       | Purpose                                                         |
| ---------------------------- | --------------------------------------------------------------- |
| `run-gateway.ps1`            | Start the API Gateway on port 5000.                             |
| `run-event-service.ps1`      | Start the Event Service on port 5001.                           |
| `run-bid-service.ps1`        | Start the Bid Scoring Service on port 5002.                     |
| `seed-data.ps1`              | Trigger seed-data manually for resets.                          |
| `clean.ps1`                  | Delete SQLite files and cached build artefacts.                 |
| `smoke-test.ps1`             | Hit `/health` on all components; exit non-zero on failure.      |
| `full-flow-test.ps1`         | Run the full happy-path flow; for pre-demo verification.        |

## Alternatives Considered

| Alternative                          | Reason for Rejection                                                                                          |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| Bash (`.sh`) scripts                 | Native on macOS and Linux; on Windows requires Git Bash or WSL — a non-trivial assumption for mixed audiences.|
| `Makefile`                           | Elegant on Unix; assumes `make` is installed, which is not standard on Windows. Better fit for Unix-only teams. |
| Both bash and PowerShell             | Two parallel script sets to maintain; doubles the surface area for divergence.                                |
| Node.js scripts (`package.json` "scripts") | Adds a Node.js prerequisite to a project that otherwise has none. Cognitive overhead.                    |
| Python scripts                       | Both stacks already include Python; low ceremony but loses the .NET ecosystem alignment that PowerShell offers. Defensible alternative. |

## Consequences

### Positive

- **One script set, three operating systems**, with no forking or duplication.
- **Microsoft ecosystem coherence** — PowerShell aligns with the .NET stack already present in the project.
- **Strong language features** — typed parameters, structured object output, proper error handling, and a mature module ecosystem.
- **Familiar to enterprise developers**, especially in Microsoft-aligned organisations.

### Negative

- **PowerShell 7+ is a prerequisite** — not pre-installed on macOS or most Linux distributions. Participants must install it (a one-line install via `brew`, `apt`, or `dnf`). Documented in the README.
- **Some Linux/macOS purists prefer Bash** for "real" scripting and may view PowerShell as foreign.
- **Editor support varies** — VS Code's PowerShell extension is excellent; some other editors are weaker.

### Neutral

- The script logic is simple enough that re-implementing in Bash, Make, or Python would be a straightforward exercise if a future team prefers a different toolchain.

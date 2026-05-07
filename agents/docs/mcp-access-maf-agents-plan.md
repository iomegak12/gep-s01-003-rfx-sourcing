# Plan — Agent Framework notebook with two MCP servers

## Context

Ramkumar wants a runnable Jupyter notebook that demonstrates the Microsoft Agent Framework (Python) wired against **both** internal MCP servers in this repo:

- **Ticket Management MCP** (Python FastMCP, Streamable HTTP at `http://localhost:8989/mcp`, **no auth**) — 5 tools, 3 resources, 3 prompts.
- **Event Service MCP** (.NET 9, Streamable HTTP at `http://localhost:5005/mcp`, **Bearer JWT pass-through**) — 14 tools, 3 resources, 4 prompts.

The notebook must showcase: agent creation with `FoundryChatClient`, multi-turn `AgentSession`, response streaming, and `client.get_mcp_tool(...)` with `approval_mode="never_require"` for both servers. Demonstration is an **end-to-end story flow** (create event → invite suppliers → raise tickets → resolve) so that, taken together, every tool/resource/prompt of both servers is exercised at least once.

Decisions locked from MCQ:

| # | Choice | Notes |
|---|--------|-------|
| C1 | Chat client = `FoundryChatClient` | Env vars `FOUNDRY_PROJECT_ENDPOINT`, `FOUNDRY_MODEL`, `AzureCliCredential`. |
| C2 | Event Service JWT via real `/auth/login` | `POST http://localhost:5003/api/v1/auth/login` with seed creds `buyer.user` / `Buyer@123` → `accessToken`. Token forwarded to Event MCP as `Authorization: Bearer …` header on the hosted MCP tool. |
| C3 | `approval_mode="never_require"` on both servers | Auto-execute everything; no human-in-the-loop pauses in the demo. |
| C4 | End-to-end story flow | One narrative across both servers; aim to touch every capability inside the story. |

## Target file

`agents/notebooks/agent_with_mcp_servers.ipynb` (new — directory does not yet exist; create it).

## Notebook outline (cells, in order)

1. **Markdown — title & overview.** What the notebook proves; service URLs; prerequisite checklist (Auth Service `:5003`, Ticket MCP `:8989`, Event Service MCP `:5005` all running; `az login` done; `.env` populated).
2. **Code — install/imports.** `%pip install agent-framework azure-identity python-dotenv httpx` (commented; only run on first launch). Imports: `os`, `asyncio`, `httpx`, `load_dotenv`, `Agent`, `FoundryChatClient`, `AzureCliCredential`.
3. **Code — load config.** `load_dotenv()`; resolve `FOUNDRY_PROJECT_ENDPOINT`, `FOUNDRY_MODEL`, `AUTH_SERVICE_URL` (default `http://localhost:5003`), `EVENT_MCP_URL` (default `http://localhost:5005/mcp`), `TICKET_MCP_URL` (default `http://localhost:8989/mcp`), `BUYER_USERNAME`/`BUYER_PASSWORD` (default `buyer.user`/`Buyer@123`). Print a sanitised summary.
4. **Markdown — Step 1: get a JWT from the Auth Service.**
5. **Code — `login()` helper.** Async `httpx.AsyncClient` POST to `/api/v1/auth/login`, returns `accessToken`. Raise with a helpful message if `:5003` isn't reachable.
6. **Markdown — Step 2: build the Foundry chat client and hosted MCP tools.**
7. **Code — chat client + MCP tools.** Open `AzureCliCredential()` (kept alive across the notebook via a top-level `async` cell that stores `credential` in a global), build `FoundryChatClient(credential=credential, project_endpoint=..., model=...)`, then:
   - `ticket_mcp = client.get_mcp_tool(name="Ticket Management MCP", url=TICKET_MCP_URL, approval_mode="never_require")`
   - `event_mcp = client.get_mcp_tool(name="Event Service MCP", url=EVENT_MCP_URL, headers={"Authorization": f"Bearer {access_token}"}, approval_mode="never_require")`
8. **Markdown — Step 3: create the agent and a session.** Explain `instructions` framing the assistant as an RFx sourcing copilot that can manage events and tickets; mention `agent.create_session()` for cross-turn memory.
9. **Code — agent + session.** `Agent(client=..., name="RfxSourcingCopilot", instructions=..., tools=[ticket_mcp, event_mcp])` opened with `async with`; `session = agent.create_session()`. Define a tiny `await ask(prompt)` helper that runs `agent.run(prompt, session=session)` and prints; and `await ask_stream(prompt)` that iterates `agent.run(..., stream=True)` printing chunks as they arrive.
10. **Markdown — End-to-end story flow.** Brief narrative: a buyer drafts and publishes a sourcing event, invites suppliers, then raises and resolves operational tickets along the way. Each turn explicitly references which MCP capabilities should be hit.
11. **Code — Turn 1 (event lifecycle setup).** Stream prompt asking the agent to: list current suppliers, draft a new event titled "Industrial Pumps Q3", add three line items, list the line items, and show the event status reference document. → exercises `list_suppliers`, `create_event`, `add_line_item`, `list_line_items`, `reference://event-status` resource.
12. **Code — Turn 2 (publish readiness via prompt).** Non-streaming. Ask the agent to "use the pre-publish readiness check prompt for the event you just created and tell me what's missing". → exercises `pre_publish_readiness_check` prompt + `get_event` + (likely) `list_invitations`/`list_line_items`.
13. **Code — Turn 3 (invite suppliers + supplier shortlist prompt).** Streaming. Ask the agent to invite the top three suppliers from the master list and use the `supplier_shortlist` prompt to justify the picks. → exercises `suppliers://master-list` resource, `invite_supplier`, `supplier_shortlist` prompt, `list_invitations`.
14. **Code — Turn 4 (revoke + publish).** Non-streaming. Ask the agent to revoke one invitation, publish the event, and read back the `event://{id}` resource. → exercises `revoke_invitation`, `publish_event`, `event://{id}` resource.
15. **Code — Turn 5 (raise tickets — Ticket MCP entry point).** Streaming. Ask the agent to register two tickets (one HIGH, one MEDIUM) about issues encountered during publishing, both `raised_by="Ramkumar"`, `domain="event-service"`, then run the `triage_new_ticket` prompt on a third issue and register it with the recommended priority. → exercises `register_ticket`, `triage_new_ticket` prompt.
16. **Code — Turn 6 (search + resources).** Non-streaming. Ask the agent for: ticket stats, the 5 most recent tickets, all OPEN tickets, and a search for tickets containing the word "publish" with priority HIGH. → exercises `tickets://stats`, `tickets://recent`, `tickets://by-status/OPEN`, `search_tickets`, `get_tickets_by_person`.
17. **Code — Turn 7 (resolve + close + standup).** Streaming. Ask the agent to resolve all tickets raised by Ramkumar (resolved_by="Ramkumar"), close the resolved ones, then run the `daily_standup_report` prompt for Ramkumar, and finally summarise everything still OPEN using `summarize_open_tickets`. → exercises `resolve_ticket`, `close_ticket`, `daily_standup_report`, `summarize_open_tickets`.
18. **Code — Turn 8 (event status summary + memory check).** Non-streaming. Ask "using the `event_status_summary` prompt, give me a final read of the event we created earlier — and confirm you remember its ID without me telling you". → exercises `event_status_summary` prompt and proves `AgentSession` retained context across all earlier turns.
19. **Code — Turn 9 (health check).** `health_check` Event MCP tool to round out the 14-tool set. Streamed.
20. **Markdown — Capability coverage matrix.** Table listing every Ticket-MCP and Event-MCP tool/resource/prompt with the turn number where it is invoked, so the reader can verify completeness at a glance.
21. **Markdown — Cleanup notes.** How to stop the running services; mention that the `async with` block closes the agent + MCP connections on cell exit and that the credential can be closed by `await credential.close()` if it was kept open at notebook level.

## Critical files / references reused

- Auth login contract: [services/authentication-service/docs/Auth_Service_Specification.md:119](services/authentication-service/docs/Auth_Service_Specification.md#L119) — request/response shape.
- Seed credentials: [services/authentication-service/README.md:29](services/authentication-service/README.md#L29).
- Ticket MCP capability list: [mcp_servers/ticket-management-mcp/README.md:95](mcp_servers/ticket-management-mcp/README.md#L95).
- Event MCP capability list (14 tools / 3 resources / 4 prompts): [mcp_servers/event-service-mcp/docs/event-service-mcp-server.md:85](mcp_servers/event-service-mcp/docs/event-service-mcp-server.md#L85).
- Pattern for `client.get_mcp_tool(...)` with headers + approval_mode: per the MS Learn `hosted-mcp-tools` page (GitHub PAT example) — same shape applies for our Bearer.

No code in this repo is being modified — the notebook is purely additive under `agents/notebooks/`.

## Verification

End-to-end checklist (execute the notebook top-to-bottom):

1. **Prereqs running** — `curl http://localhost:5003/api/v1/health`, `curl http://localhost:8989/mcp` (expect 405/SSE-style hint), `curl http://localhost:5005/api/v1/health` all reachable; `az account show` returns the right tenant.
2. **`.env` populated** — `FOUNDRY_PROJECT_ENDPOINT`, `FOUNDRY_MODEL`, optionally overrides for service URLs. Notebook prints a sanitised summary in cell 3.
3. **Login cell** — returns a non-empty `accessToken`; print only `len(token)` to avoid leaking it.
4. **Agent + tools cell** — no exceptions; agent enters `async with` block.
5. **Turns 1–9** — each cell prints a non-empty agent reply; streaming cells visibly emit chunks; turn 8 confirms session memory by referring to the earlier event ID without the user re-supplying it.
6. **Coverage matrix** in cell 20 — every row is checked off (this is a manual scan against the README/spec lists).
7. **Negative spot check** — temporarily stop `:5005`, re-run turn 9; the agent should surface an upstream error rather than crash the notebook.

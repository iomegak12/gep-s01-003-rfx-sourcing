"""MCP prompts — reusable message templates for common LLM interactions."""

from __future__ import annotations

import json

from fastmcp import FastMCP
from fastmcp.prompts import Message

from ticket_mgmt.repositories import ticket_repo


def register_all(mcp: FastMCP) -> None:

    @mcp.prompt(
        name="summarize_open_tickets",
        description=(
            "Generates a prompt asking the LLM to summarize all currently open and "
            "in-progress tickets, grouped by priority. Embeds a live snapshot of the data."
        ),
        tags={"summary", "operations"},
    )
    def summarize_open_tickets() -> str:
        """Summarize open and in-progress tickets grouped by priority."""
        open_tickets = ticket_repo.by_status("OPEN")
        in_progress_tickets = ticket_repo.by_status("IN_PROGRESS")
        all_active = open_tickets + in_progress_tickets

        snapshot = json.dumps(
            [
                {
                    "ticket_id": t.ticket_id,
                    "description": t.description,
                    "raised_by": t.raised_by,
                    "priority": t.priority,
                    "status": t.status,
                    "domain": t.domain,
                    "registered_date": t.registered_date.isoformat() if t.registered_date else None,
                }
                for t in all_active
            ],
            indent=2,
        )

        return (
            f"The following {len(all_active)} ticket(s) are currently OPEN or IN_PROGRESS:\n\n"
            f"```json\n{snapshot}\n```\n\n"
            "Please summarize these tickets, grouping them by priority (CRITICAL → HIGH → MEDIUM → LOW). "
            "For each group, list the ticket IDs, brief descriptions, who raised them, and any domain information. "
            "Highlight any CRITICAL tickets prominently at the top."
        )

    @mcp.prompt(
        name="daily_standup_report",
        description=(
            "Generates a daily stand-up prompt for a specific person, listing their "
            "open tickets and recently resolved/closed tickets."
        ),
        tags={"standup", "reporting"},
    )
    def daily_standup_report(
        person_name: str,
    ) -> list[Message]:
        """Generate a stand-up report for a given person."""
        all_tickets = ticket_repo.find_by_person(person_name, role="any")
        open_tickets = [t for t in all_tickets if t.status in ("OPEN", "IN_PROGRESS")]
        resolved_tickets = [t for t in all_tickets if t.status in ("RESOLVED", "CLOSED")]

        open_snapshot = json.dumps(
            [{"ticket_id": t.ticket_id, "description": t.description, "priority": t.priority, "status": t.status} for t in open_tickets],
            indent=2,
        )
        resolved_snapshot = json.dumps(
            [{"ticket_id": t.ticket_id, "description": t.description, "status": t.status, "resolved_by": t.resolved_by} for t in resolved_tickets],
            indent=2,
        )

        user_msg = (
            f"Generate a daily stand-up report for **{person_name}**.\n\n"
            f"**Open / In-Progress tickets ({len(open_tickets)}):**\n```json\n{open_snapshot}\n```\n\n"
            f"**Recently Resolved / Closed tickets ({len(resolved_tickets)}):**\n```json\n{resolved_snapshot}\n```\n\n"
            "Format the report with:\n"
            "1. What was completed (resolved/closed tickets)\n"
            "2. What is in progress (open/in-progress tickets), with priorities\n"
            "3. Any blockers or CRITICAL items that need attention\n"
            "Keep it concise and suitable for a team stand-up."
        )
        assistant_msg = (
            f"Understood. I'll generate a stand-up report for {person_name} based on the ticket data provided."
        )

        return [
            Message(user_msg, role="user"),
            Message(assistant_msg, role="assistant"),
        ]

    @mcp.prompt(
        name="triage_new_ticket",
        description=(
            "Given a ticket description, asks the LLM to recommend an appropriate "
            "priority level and suggest the likely domain/service."
        ),
        tags={"triage", "classification"},
    )
    def triage_new_ticket(
        description: str,
    ) -> str:
        """Ask the LLM to recommend priority and domain for a new ticket."""
        return (
            f"A new support ticket has been submitted with the following description:\n\n"
            f"---\n{description}\n---\n\n"
            "Based on the description, please:\n"
            "1. **Recommend a priority** from: LOW, MEDIUM, HIGH, or CRITICAL — and explain your reasoning.\n"
            "2. **Suggest the most likely domain or service** this ticket belongs to "
            "(e.g. event-service, bid-scoring-service, authentication-service, or other).\n"
            "3. **Suggest a concise ticket title** (max 10 words) that captures the core issue.\n\n"
            "Format your response as:\n"
            "- **Priority:** <value> — <reason>\n"
            "- **Domain:** <value>\n"
            "- **Suggested Title:** <title>"
        )

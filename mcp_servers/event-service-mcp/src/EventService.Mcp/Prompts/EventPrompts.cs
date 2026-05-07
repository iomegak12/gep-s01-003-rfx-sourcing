using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Prompts;

[McpServerPromptType]
public sealed class EventPrompts
{
    [McpServerPrompt(Name = "draft_sourcing_event")]
    [Description("Turns a free-text sourcing brief into a concrete CreateEventRequest payload plus suggested line items.")]
    public static IEnumerable<ChatMessage> DraftSourcingEvent(
        [Description("Free-text sourcing brief, e.g. 'I need to source 50 enterprise laptops by August 15.'")]
        string brief)
    {
        const string system =
            "You are a procurement assistant operating against the RFx Sourcing Event Service. " +
            "Your job is to turn a free-text sourcing brief into a structured payload usable by the create_event tool, " +
            "plus one or more line items usable by the add_line_item tool. " +
            "Rules: " +
            "(1) responseDeadlineUtc MUST be ISO-8601 UTC and strictly in the future. " +
            "(2) currency MUST be a valid ISO-4217 code; default to INR if not stated. " +
            "(3) category MUST be a single short token (e.g. 'IT', 'Logistics', 'Office'). " +
            "(4) For each distinct item in the brief, propose a line item with description, quantity, and a reasonable unitPrice estimate (mark estimate=true). " +
            "Output a single JSON object with two top-level keys: 'event' and 'lineItems'.";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Brief:\n{brief}")
        };
    }

    [McpServerPrompt(Name = "pre_publish_readiness_check")]
    [Description("Walks through the local publish gates for a given event ID and reports what is missing.")]
    public static IEnumerable<ChatMessage> PrePublishReadinessCheck(
        [Description("The event ID to check.")] string eventId)
    {
        const string system =
            "You are validating whether a sourcing event is ready to publish. " +
            "Use the available MCP tools and resources in this exact order: " +
            "(1) Read the resource reference://event-status to recall the gates. " +
            "(2) Call get_event with the supplied ID. " +
            "(3) Call list_line_items with the same ID. " +
            "(4) Call list_invitations with the same ID. " +
            "Then produce a concise checklist: for each of the four gates, mark PASS or FAIL with a one-line reason. " +
            "If all four pass, recommend calling publish_event. If any fail, list the exact remediation steps.";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Event ID: {eventId}")
        };
    }

    [McpServerPrompt(Name = "supplier_shortlist")]
    [Description("Suggests suppliers from the master list that match a category and free-text criteria.")]
    public static IEnumerable<ChatMessage> SupplierShortlist(
        [Description("The sourcing category, e.g. 'IT' or 'Logistics'.")] string category,
        [Description("Free-text criteria, e.g. 'GST-registered, India, prior cloud experience'.")] string criteria)
    {
        const string system =
            "You are recommending suppliers for an upcoming sourcing event. " +
            "Procedure: " +
            "(1) Read the resource suppliers://master-list to obtain the candidate set. " +
            "(2) Filter to active suppliers whose attributes match the category and criteria. " +
            "(3) Return up to 5 suppliers, ranked, with a one-line justification each. " +
            "(4) For each shortlisted supplier, include the supplierId so the user can pass it to invite_supplier. " +
            "Be conservative: do NOT invent supplier capabilities not present in the master-list payload.";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Category: {category}\nCriteria: {criteria}")
        };
    }

    [McpServerPrompt(Name = "event_status_summary")]
    [Description("Generates a stakeholder-friendly status summary for a given event.")]
    public static IEnumerable<ChatMessage> EventStatusSummary(
        [Description("The event ID to summarise.")] string eventId,
        [Description("Audience - 'executive' for a 3-line summary, 'operational' for a detailed breakdown.")]
        string audience = "executive")
    {
        const string system =
            "You are producing a status summary for a sourcing event. " +
            "Procedure: " +
            "(1) Call get_event for the supplied ID. " +
            "(2) Call list_line_items and list_invitations. " +
            "(3) Compute days-to-deadline from responseDeadlineUtc and 'today' (UTC). " +
            "(4) Render the summary in the requested format: " +
            "    - 'executive': three lines - status, totals (line items / invitees), days to deadline. " +
            "    - 'operational': bullet list with status, deadline, line-item count and total estimated value, " +
            "      invitee count with names, and any open risks (e.g. deadline within 7 days, no invitations).";

        return new[]
        {
            new ChatMessage(ChatRole.System, system),
            new ChatMessage(ChatRole.User,   $"Event ID: {eventId}\nAudience: {audience}")
        };
    }
}

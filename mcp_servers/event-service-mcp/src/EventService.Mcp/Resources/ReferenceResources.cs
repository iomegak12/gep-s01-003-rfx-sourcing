using System.ComponentModel;
using System.Text.Json;
using EventService.Mcp.HttpClients;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Resources;

[McpServerResourceType]
public sealed class ReferenceResources
{
    [McpServerResource(
        UriTemplate = "reference://event-status",
        Name = "Event Status Reference",
        MimeType = "application/json")]
    [Description("Static reference: EventStatus enum values, valid transitions, and the local publish-gate checklist.")]
    public static ResourceContents GetEventStatusReference()
    {
        var doc = new
        {
            statuses = new[] { "Draft", "Published", "Bidding", "Scored", "Awarded", "Closed" },
            reachableInPhase1 = new[] { "Draft", "Published" },
            transitions = new[]
            {
                new { from = "Draft", to = "Published", trigger = "publish_event tool" }
            },
            publishGates = new[]
            {
                "Event must be in Draft status",
                "responseDeadlineUtc must be strictly in the future",
                "At least one line item must exist",
                "At least one supplier must be invited"
            },
            notEnforcedYet = new[]
            {
                "BSS scoring-criteria gate (deferred to Phase 6 of the upstream impl plan)"
            }
        };

        return new TextResourceContents
        {
            Uri = "reference://event-status",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(doc, EventServiceClient.JsonOpts)
        };
    }
}

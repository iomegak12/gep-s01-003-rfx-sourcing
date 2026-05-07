using System.ComponentModel;
using System.Text.Json;
using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Tools;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Resources;

[McpServerResourceType]
public sealed class EventResources
{
    private readonly IEventServiceClient _client;

    public EventResources(IEventServiceClient client) => _client = client;

    [McpServerResource(
        UriTemplate = "event://{id}",
        Name = "Sourcing Event",
        MimeType = "application/json")]
    [Description("Returns a single sourcing event (header, status, deadline, currency, category) by ID.")]
    public async Task<ResourceContents> GetEvent(
        [Description("The event's UUID.")] string id,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new McpException($"Invalid event ID: {id}");

        try
        {
            var ev = await _client.GetEventAsync(guid, ct);
            return new TextResourceContents
            {
                Uri = $"event://{id}",
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(ev, EventServiceClient.JsonOpts)
            };
        }
        catch (UpstreamException ex) when (ex.StatusCode == 404)
        {
            throw new McpException($"Event not found: {id}");
        }
        catch (UpstreamException ex)
        {
            throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex));
        }
    }
}

using System.ComponentModel;
using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Responses;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Tools;

[McpServerToolType]
public sealed class HealthTools
{
    private readonly IEventServiceHealthClient _client;

    public HealthTools(IEventServiceHealthClient client) => _client = client;

    [McpServerTool(Name = "health_check")]
    [Description("Liveness probe for the upstream Event Service. Does not require authentication.")]
    public async Task<HealthResponse> HealthCheck(CancellationToken ct = default)
    {
        try { return await _client.GetHealthAsync(ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }
}

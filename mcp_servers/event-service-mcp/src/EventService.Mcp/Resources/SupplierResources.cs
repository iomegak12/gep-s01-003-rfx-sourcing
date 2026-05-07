using System.ComponentModel;
using System.Text.Json;
using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Responses;
using EventService.Mcp.Tools;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Resources;

[McpServerResourceType]
public sealed class SupplierResources
{
    private const int PageSize = 100;
    private const int HardCap = 1000;

    private readonly IEventServiceClient _client;

    public SupplierResources(IEventServiceClient client) => _client = client;

    [McpServerResource(
        UriTemplate = "suppliers://master-list",
        Name = "Supplier Master List",
        MimeType = "application/json")]
    [Description("Returns the full list of active suppliers from the organisation-wide master list. Paginated server-side; capped at 1000 items.")]
    public async Task<ResourceContents> GetMasterList(CancellationToken ct = default)
    {
        var all = new List<SupplierResponse>();
        int offset = 0;
        try
        {
            while (all.Count < HardCap)
            {
                var page = await _client.ListSuppliersAsync(PageSize, offset, activeOnly: true, ct);
                if (page.Items.Count == 0) break;
                all.AddRange(page.Items);
                if (page.Items.Count < PageSize) break;
                offset += PageSize;
            }
        }
        catch (UpstreamException ex)
        {
            throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex));
        }

        return new TextResourceContents
        {
            Uri = "suppliers://master-list",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { count = all.Count, items = all }, EventServiceClient.JsonOpts)
        };
    }
}

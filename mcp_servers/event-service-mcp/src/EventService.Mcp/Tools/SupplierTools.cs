using System.ComponentModel;
using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Responses;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Tools;

[McpServerToolType]
public sealed class SupplierTools
{
    private readonly IEventServiceClient _client;

    public SupplierTools(IEventServiceClient client) => _client = client;

    [McpServerTool(Name = "list_suppliers")]
    [Description("Returns a paginated list of suppliers from the organisation-wide master list.")]
    public async Task<SupplierListResponse> ListSuppliers(
        [Description("Maximum number of suppliers to return (1-100). Defaults to 50.")] int limit = 50,
        [Description("Zero-based offset for pagination. Defaults to 0.")] int offset = 0,
        [Description("If true, only active suppliers are returned. Defaults to true.")] bool activeOnly = true,
        CancellationToken ct = default)
    {
        try { return await _client.ListSuppliersAsync(limit, offset, activeOnly, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "get_supplier")]
    [Description("Returns a single supplier by its ID.")]
    public async Task<SupplierResponse> GetSupplier(
        [Description("The supplier's UUID.")] Guid id,
        CancellationToken ct = default)
    {
        try { return await _client.GetSupplierAsync(id, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }
}

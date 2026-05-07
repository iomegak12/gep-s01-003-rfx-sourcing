using System.ComponentModel;
using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Tools;

[McpServerToolType]
public sealed class LineItemTools
{
    private readonly IEventServiceClient _client;

    public LineItemTools(IEventServiceClient client) => _client = client;

    [McpServerTool(Name = "add_line_item")]
    [Description("Adds a line item (description, quantity, unit price) to a Draft event.")]
    public async Task<LineItemResponse> AddLineItem(
        [Description("The event's UUID.")] Guid eventId,
        [Description("Line-item description, quantity, and unit price.")] AddLineItemRequest request,
        CancellationToken ct = default)
    {
        try { return await _client.AddLineItemAsync(eventId, request, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "list_line_items")]
    [Description("Returns all line items for the specified event.")]
    public async Task<LineItemListResponse> ListLineItems(
        [Description("The event's UUID.")] Guid eventId,
        CancellationToken ct = default)
    {
        try { return await _client.ListLineItemsAsync(eventId, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "delete_line_item")]
    [Description("Removes a line item from a Draft event. Returns a confirmation string on success.")]
    public async Task<string> DeleteLineItem(
        [Description("The event's UUID.")] Guid eventId,
        [Description("The line item's UUID.")] Guid itemId,
        CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteLineItemAsync(eventId, itemId, ct);
            return $"Line item {itemId} removed from event {eventId}.";
        }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }
}

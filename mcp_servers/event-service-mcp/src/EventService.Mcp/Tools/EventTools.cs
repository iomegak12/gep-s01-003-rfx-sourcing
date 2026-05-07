using System.ComponentModel;
using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Tools;

[McpServerToolType]
public sealed class EventTools
{
    private readonly IEventServiceClient _client;

    public EventTools(IEventServiceClient client) => _client = client;

    [McpServerTool(Name = "create_event")]
    [Description("Creates a new sourcing event in Draft status. Returns the created event including its generated ID.")]
    public async Task<EventResponse> CreateEvent(
        [Description("Title, description, category, currency, and response deadline (UTC, ISO-8601).")]
        CreateEventRequest request,
        CancellationToken ct = default)
    {
        try { return await _client.CreateEventAsync(request, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "list_events")]
    [Description("Returns a paginated list of sourcing events.")]
    public async Task<EventListResponse> ListEvents(
        [Description("Maximum number of events to return. Defaults to 20.")] int limit = 20,
        [Description("Zero-based offset for pagination. Defaults to 0.")] int offset = 0,
        CancellationToken ct = default)
    {
        try { return await _client.ListEventsAsync(limit, offset, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "get_event")]
    [Description("Returns a single sourcing event by ID.")]
    public async Task<EventResponse> GetEvent(
        [Description("The event's UUID.")] Guid id,
        CancellationToken ct = default)
    {
        try { return await _client.GetEventAsync(id, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "update_event")]
    [Description("Updates a Draft sourcing event. Returns 409 (rejected) if the event is no longer in Draft status.")]
    public async Task<EventResponse> UpdateEvent(
        [Description("The event's UUID.")] Guid id,
        [Description("Optional new title, description, category, and/or response deadline.")]
        UpdateEventRequest request,
        CancellationToken ct = default)
    {
        try { return await _client.UpdateEventAsync(id, request, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "publish_event")]
    [Description(
        "Publishes a Draft event, transitioning its status to Published. " +
        "Local publish gates: event must be Draft; responseDeadlineUtc must be strictly in the future; " +
        "at least one line item must exist; at least one supplier must be invited. " +
        "If any gate fails the call returns a rejection (409) describing the failed gate.")]
    public async Task<EventResponse> PublishEvent(
        [Description("The event's UUID.")] Guid id,
        CancellationToken ct = default)
    {
        try { return await _client.PublishEventAsync(id, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }
}

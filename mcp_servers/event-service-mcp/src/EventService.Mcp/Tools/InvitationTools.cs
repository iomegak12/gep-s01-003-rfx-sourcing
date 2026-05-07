using System.ComponentModel;
using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EventService.Mcp.Tools;

[McpServerToolType]
public sealed class InvitationTools
{
    private readonly IEventServiceClient _client;

    public InvitationTools(IEventServiceClient client) => _client = client;

    [McpServerTool(Name = "invite_supplier")]
    [Description("Invites an active supplier to a Draft event. Rejected (409) if the event is not Draft, the supplier is inactive, or the supplier is already invited.")]
    public async Task<InvitationResponse> InviteSupplier(
        [Description("The event's UUID.")] Guid eventId,
        [Description("The supplier ID to invite.")] InviteSupplierRequest request,
        CancellationToken ct = default)
    {
        try { return await _client.InviteSupplierAsync(eventId, request, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "list_invitations")]
    [Description("Returns all supplier invitations for the specified event.")]
    public async Task<InvitationListResponse> ListInvitations(
        [Description("The event's UUID.")] Guid eventId,
        CancellationToken ct = default)
    {
        try { return await _client.ListInvitationsAsync(eventId, ct); }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }

    [McpServerTool(Name = "revoke_invitation")]
    [Description("Revokes a supplier invitation from a Draft event. Returns a confirmation string on success.")]
    public async Task<string> RevokeInvitation(
        [Description("The event's UUID.")] Guid eventId,
        [Description("The supplier's UUID.")] Guid supplierId,
        CancellationToken ct = default)
    {
        try
        {
            await _client.RevokeInvitationAsync(eventId, supplierId, ct);
            return $"Invitation for supplier {supplierId} revoked from event {eventId}.";
        }
        catch (UpstreamException ex) { throw new McpException(ToolErrorFormatting.FormatUpstreamMessage(ex)); }
    }
}

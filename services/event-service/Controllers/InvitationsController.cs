using EventService.Infrastructure.Logging;
using EventService.Services.Invitations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers;

/// <summary>
/// Manages supplier invitations for a sourcing event.
/// Invitations may only be sent or revoked while the event is in Draft status.
/// </summary>
[ApiController]
[Route("api/v1/events/{eventId:guid}/invitations")]
[Authorize]
[Produces("application/json")]
public sealed class InvitationsController : ControllerBase
{
    private readonly IInvitationService _service;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(IInvitationService service, ILogger<InvitationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Invites an active supplier to a Draft event.
    /// Returns 409 if the event is not Draft, the supplier is inactive, or the supplier is already invited.
    /// </summary>
    /// <response code="201">Invitation created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event or supplier not found.</response>
    /// <response code="409">Event not in Draft, supplier inactive, or already invited.</response>
    [HttpPost]
    [ProducesResponseType(typeof(InvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteAsync(
        Guid eventId,
        [FromBody] InviteSupplierRequest request,
        CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        var correlationId = HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s ? s : string.Empty;

        var result = await _service.InviteAsync(eventId, request.SupplierId, userId, correlationId, ct);

        return Created($"/api/v1/events/{eventId}/invitations", result);
    }

    /// <summary>Returns all supplier invitations for the specified event.</summary>
    /// <response code="200">List returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(InvitationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAsync(Guid eventId, CancellationToken ct)
    {
        var result = await _service.ListByEventAsync(eventId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Revokes a supplier invitation from a Draft event.
    /// Returns 409 if the event is not in Draft status.
    /// </summary>
    /// <response code="204">Invitation revoked.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event not found or supplier not invited.</response>
    /// <response code="409">Event is not in Draft status.</response>
    [HttpDelete("{supplierId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeAsync(
        Guid eventId,
        Guid supplierId,
        CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        var correlationId = HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s ? s : string.Empty;

        await _service.RevokeAsync(eventId, supplierId, userId, correlationId, ct);
        return NoContent();
    }
}

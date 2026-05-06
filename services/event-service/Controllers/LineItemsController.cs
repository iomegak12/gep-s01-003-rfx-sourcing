using EventService.Infrastructure.Logging;
using EventService.Services.LineItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers;

/// <summary>
/// Manages line items belonging to a sourcing event.
/// Line items may only be added to or removed from events in Draft status.
/// </summary>
[ApiController]
[Route("api/v1/events/{eventId:guid}/line-items")]
[Authorize]
[Produces("application/json")]
public sealed class LineItemsController : ControllerBase
{
    private readonly ILineItemService _service;
    private readonly ILogger<LineItemsController> _logger;

    public LineItemsController(ILineItemService service, ILogger<LineItemsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Adds a line item to a Draft event.</summary>
    /// <response code="201">Line item created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event is not in Draft status.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LineItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddAsync(
        Guid eventId,
        [FromBody] AddLineItemRequest request,
        CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        var correlationId = HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s ? s : string.Empty;

        var result = await _service.AddAsync(eventId, request, userId, correlationId, ct);

        return Created($"/api/v1/events/{eventId}/line-items", result);
    }

    /// <summary>Returns all line items for an event.</summary>
    /// <response code="200">List returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(LineItemListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAsync(Guid eventId, CancellationToken ct)
    {
        var result = await _service.ListByEventAsync(eventId, ct);
        return Ok(result);
    }

    /// <summary>Removes a line item from a Draft event.</summary>
    /// <response code="204">Line item deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event or line item not found.</response>
    /// <response code="409">Event is not in Draft status.</response>
    [HttpDelete("{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        Guid eventId,
        Guid itemId,
        CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        var correlationId = HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s ? s : string.Empty;

        await _service.DeleteAsync(eventId, itemId, userId, correlationId, ct);
        return NoContent();
    }
}

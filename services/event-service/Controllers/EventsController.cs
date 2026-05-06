using EventService.Infrastructure.Logging;
using EventService.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers;

/// <summary>
/// Manages the sourcing-event lifecycle (create, list, get, update).
/// Publish and award endpoints are defined in later phases.
/// </summary>
[ApiController]
[Route("api/v1/events")]
[Authorize]
[Produces("application/json")]
public sealed class EventsController : ControllerBase
{
    private readonly IEventService _service;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService service, ILogger<EventsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Creates a new sourcing event in Draft status.</summary>
    /// <response code="201">Event created successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateEventRequest request,
        CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        var correlationId = HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s ? s : string.Empty;

        var result = await _service.CreateAsync(request, userId, correlationId, ct);

        return Created($"/api/v1/events/{result.Id}", result);
    }

    /// <summary>Returns a paginated list of sourcing events.</summary>
    /// <response code="200">List returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    [HttpGet]
    [ProducesResponseType(typeof(EventListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(limit, offset, ct);
        return Ok(result);
    }

    /// <summary>Returns a single sourcing event by ID.</summary>
    /// <response code="200">Event found.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates a Draft sourcing event. Returns 409 if the event is not in Draft status.
    /// </summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event is not in Draft status.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        var correlationId = HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s ? s : string.Empty;

        var result = await _service.UpdateAsync(id, request, userId, correlationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Publishes a Draft event, transitioning its status to Published.
    /// All local gates must pass: event is Draft, deadline is in the future,
    /// at least one line item exists, at least one supplier is invited.
    /// The BSS scoring-criteria gate is intentionally not enforced in this build
    /// — see docs/impl-event-service-plan.md Phase 6 (TODO(BSS-INTEGRATION)).
    /// </summary>
    /// <response code="200">Event published successfully.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">One or more publish gates failed.</response>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishAsync(Guid id, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        var correlationId = HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) && v is string s ? s : string.Empty;

        var result = await _service.PublishAsync(id, userId, correlationId, ct);
        return Ok(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers;

/// <summary>
/// Liveness probe. Returns <c>200 OK</c> if the process is running. Anonymous.
/// </summary>
[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    /// <summary>Returns service liveness status.</summary>
    /// <response code="200">Service is alive.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "event-service",
        timestamp = DateTime.UtcNow.ToString("O")
    });
}

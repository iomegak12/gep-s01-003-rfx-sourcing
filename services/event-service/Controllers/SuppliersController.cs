using EventService.Services.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers;

/// <summary>Read-only endpoints over the supplier master list.</summary>
[ApiController]
[Route("api/v1/suppliers")]
[Authorize]
[Produces("application/json")]
public sealed class SuppliersController : ControllerBase
{
    private readonly ISupplierService _suppliers;

    public SuppliersController(ISupplierService suppliers)
    {
        _suppliers = suppliers;
    }

    /// <summary>Lists suppliers, paginated, ordered by name.</summary>
    /// <param name="activeOnly">If true (default) only active suppliers are returned.</param>
    /// <param name="limit">Page size [1, 100]. Default 50.</param>
    /// <param name="offset">Number of suppliers to skip. Default 0.</param>
    /// <response code="200">A page of suppliers.</response>
    /// <response code="401">Missing or invalid token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SupplierListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<SupplierListResponse> List(
        [FromQuery] bool activeOnly = true,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
        => _suppliers.ListAsync(activeOnly, limit, offset, cancellationToken);

    /// <summary>Returns a single supplier by id.</summary>
    /// <param name="id">Supplier id (UUIDv7).</param>
    /// <response code="200">The supplier.</response>
    /// <response code="404">Supplier not found.</response>
    /// <response code="401">Missing or invalid token.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<SupplierResponse> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => _suppliers.GetByIdAsync(id, cancellationToken);
}

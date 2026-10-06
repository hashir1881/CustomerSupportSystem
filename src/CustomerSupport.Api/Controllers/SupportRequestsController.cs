using CustomerSupport.Application.Dtos;
using CustomerSupport.Application.Services;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/support-requests")]
public class SupportRequestsController : ControllerBase
{
    private readonly ISupportRequestService _supportRequestService;

    public SupportRequestsController(ISupportRequestService supportRequestService)
    {
        _supportRequestService = supportRequestService;
    }

    /// <summary>
    /// Gets complaints (filtered by status optionally).
    /// Customers see their own complaints. CS agents see all matching complaints.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupportRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupportRequestDto>>> GetAll(
        [FromQuery] RequestStatus? status,
        CancellationToken cancellationToken)
    {
        var requests = await _supportRequestService.GetAllAsync(status, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Gets a single complaint by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupportRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SupportRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await _supportRequestService.GetByIdAsync(id, cancellationToken);
        return Ok(request);
    }

    /// <summary>
    /// Submits a new customer support complaint.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SupportRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupportRequestDto>> Create(
        [FromBody] CreateSupportRequestDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _supportRequestService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing pending complaint's subject/description.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SupportRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupportRequestDto>> Update(
        Guid id,
        [FromBody] UpdateSupportRequestDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _supportRequestService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Atomically claims an unassigned complaint. CS agents only.
    /// Returns 200 OK on success, or 409 Conflict if already claimed by another agent.
    /// </summary>
    [HttpPost("{id:guid}/claim")]
    [Authorize(Roles = "CustomerService,Administrator")]
    [ProducesResponseType(typeof(SupportRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SupportRequestDto>> Claim(Guid id, CancellationToken cancellationToken)
    {
        var claimed = await _supportRequestService.ClaimAsync(id, cancellationToken);
        return Ok(claimed);
    }

    /// <summary>
    /// Marks a complaint as resolved. CS agents only.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "CustomerService,Administrator")]
    [ProducesResponseType(typeof(SupportRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupportRequestDto>> Resolve(Guid id, CancellationToken cancellationToken)
    {
        var resolved = await _supportRequestService.ResolveAsync(id, cancellationToken);
        return Ok(resolved);
    }

    /// <summary>
    /// Closes a complaint.
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(SupportRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupportRequestDto>> Close(Guid id, CancellationToken cancellationToken)
    {
        var closed = await _supportRequestService.CloseAsync(id, cancellationToken);
        return Ok(closed);
    }
}

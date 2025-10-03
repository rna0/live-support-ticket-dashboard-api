using Microsoft.AspNetCore.Mvc;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TicketsController(ITicketRepository ticketRepository) : ControllerBase
{
    /// <summary>
    /// Get tickets with optional filtering and pagination
    /// </summary>
    /// <param name="status">Filter by ticket status (Open, InProgress, Resolved)</param>
    /// <param name="priority">Filter by priority (Low, Medium, High, Critical)</param>
    /// <param name="q">Search in ticket titles</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of tickets with pagination metadata</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Ticket>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Validate enum values if provided
        if (status is not null && !Enum.TryParse<TicketStatus>(status, ignoreCase: true, out _))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid status value",
                Detail = "Status must be one of: Open, InProgress, Resolved"
            });
        }

        if (priority is not null && !Enum.TryParse<TicketPriority>(priority, ignoreCase: true, out _))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid priority value",
                Detail = "Priority must be one of: Low, Medium, High, Critical"
            });
        }

        var (items, total) = await ticketRepository.QueryAsync(status, priority, q, page, pageSize, ct);

        // Add pagination metadata to response headers
        Response.Headers["X-Total-Count"] = total.ToString();
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-Page-Size"] = pageSize.ToString();
        Response.Headers["X-Total-Pages"] = ((int)Math.Ceiling((double)total / pageSize)).ToString();

        return Ok(items);
    }

    /// <summary>
    /// Get a specific ticket by ID
    /// </summary>
    /// <param name="id">Ticket ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The ticket if found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Ticket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(id, ct);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {id}"
            });
        }

        return Ok(ticket);
    }

    /// <summary>
    /// Create a new support ticket
    /// </summary>
    /// <param name="request">Ticket creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created ticket ID and location</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ticketId = await ticketRepository.CreateAsync(request, ct);

        return CreatedAtAction(
            nameof(GetTicket),
            new { id = ticketId },
            new { id = ticketId, message = "Ticket created successfully" });
    }

    /// <summary>
    /// Update the status of a ticket
    /// </summary>
    /// <param name="id">Ticket ID</param>
    /// <param name="request">Status update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTicketStatus(
        Guid id,
        [FromBody] UpdateTicketStatusRequest request,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await ticketRepository.UpdateStatusAsync(id, request.Status.ToString(), ct);

        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {id}"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Assign a ticket to an agent
    /// </summary>
    /// <param name="id">Ticket ID</param>
    /// <param name="request">Assignment request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignTicket(
        Guid id,
        [FromBody] AssignTicketRequest request,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await ticketRepository.AssignAsync(id, request.AgentId, ct);

        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {id}"
            });
        }

        return NoContent();
    }
}

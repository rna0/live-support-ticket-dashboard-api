using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Interfaces;
using LiveSupportDashboard.Services.Validations;
using Microsoft.AspNetCore.Mvc;

namespace LiveSupportDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IValidationService _validationService;
    private readonly INotificationService _notificationService;
    private readonly IAgentRepository _agentRepository;

    public TicketsController(
        ITicketRepository ticketRepository,
        IValidationService validationService,
        INotificationService notificationService,
        IAgentRepository agentRepository)
    {
        _ticketRepository = ticketRepository;
        _validationService = validationService;
        _notificationService = notificationService;
        _agentRepository = agentRepository;
    }

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
        // Use validation service for query parameters
        var queryParams = new TicketQueryParameters
        {
            Status = status,
            Priority = priority,
            SearchQuery = q,
            Page = page,
            PageSize = pageSize
        };

        var validationResult = await _validationService.ValidateAsync(queryParams, ct);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        var (items, total) = await _ticketRepository.QueryAsync(status, priority, q, page, pageSize, ct);

        // Add pagination metadata to response headers
        Response.Headers["X-Total-Count"] = total.ToString();
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-Page-Size"] = pageSize.ToString();
        Response.Headers["X-Total-Pages"] = ((int)Math.Ceiling((double)total / pageSize)).ToString();

        return Ok(items);
    }

    /// <summary>
    /// Get tickets with history and SLA information (frontend-compatible)
    /// </summary>
    [HttpGet("with-history")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTicketsWithHistory(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Use validation service for query parameters
        var queryParams = new TicketQueryParameters
        {
            Status = status,
            Priority = priority,
            SearchQuery = q,
            Page = page,
            PageSize = pageSize
        };

        var validationResult = await _validationService.ValidateAsync(queryParams, ct);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        var (items, total) = await _ticketRepository.QueryWithHistoryAsync(status, priority, q, page, pageSize, ct);

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
        var ticket = await _ticketRepository.GetByIdAsync(id, ct);

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
    /// Get a specific ticket with history and SLA information (frontend-compatible)
    /// </summary>
    [HttpGet("{id:guid}/with-history")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketWithHistory(Guid id, CancellationToken ct = default)
    {
        var ticketResponse = await _ticketRepository.GetTicketWithHistoryAsync(id, ct);

        if (ticketResponse is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {id}"
            });
        }

        return Ok(ticketResponse);
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
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request,
        CancellationToken ct = default)
    {
        // Use validation service instead of ModelState
        var validationResult = await _validationService.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        // If the request includes an assigned agent, validate agent assignment separately
        if (request.AssignedAgentId.HasValue)
        {
            var agentValidation = await _validationService.ValidateAsync(request.AssignedAgentId, ct);
            if (!agentValidation.IsValid)
            {
                var problemDetails = new ValidationProblemDetails();
                foreach (var error in agentValidation.Errors)
                {
                    problemDetails.Errors.Add(nameof(request.AssignedAgentId), new[] { error.Message });
                }

                return BadRequest(problemDetails);
            }
        }

        var ticketId = await _ticketRepository.CreateAsync(request, ct);

        // Get the created ticket for notification
        var createdTicket = await _ticketRepository.GetByIdAsync(ticketId, ct);
        if (createdTicket != null)
        {
            await _notificationService.NotifyTicketCreatedAsync(createdTicket);
        }

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
        // Validate the request
        var requestValidation = await _validationService.ValidateAsync(request, ct);
        if (!requestValidation.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in requestValidation.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        // Get current ticket for business rule validation
        var currentTicket = await _ticketRepository.GetByIdAsync(id, ct);
        if (currentTicket == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {id}"
            });
        }

        // Validate business rules for status update
        var operationValidation = await _validationService.ValidateTicketOperationAsync(id, "status_update", ct);
        if (!operationValidation.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in operationValidation.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        var oldStatus = currentTicket.Status;
        var success = await _ticketRepository.UpdateStatusAsync(id, request.Status.ToString(), ct);

        if (success)
        {
            // Notify status change
            await _notificationService.NotifyTicketStatusChangedAsync(id, oldStatus, request.Status);

            // Get updated ticket and notify
            var updatedTicket = await _ticketRepository.GetByIdAsync(id, ct);
            if (updatedTicket != null)
            {
                await _notificationService.NotifyTicketUpdatedAsync(updatedTicket);
            }
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
        // Validate the request
        var requestValidation = await _validationService.ValidateAsync(request, ct);
        if (!requestValidation.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in requestValidation.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        // Validate business rules for assignment
        var operationValidation = await _validationService.ValidateTicketOperationAsync(id, "assignment", ct);
        if (!operationValidation.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in operationValidation.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        var success = await _ticketRepository.AssignAsync(id, request.AgentId, ct);

        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {id}"
            });
        }

        // Notify assignment (assuming agent name is available from agent service)
        var agent = await _agentRepository.GetByIdAsync(request.AgentId, ct);
        var agentName = agent?.Name ?? "Unknown Agent";
        await _notificationService.NotifyTicketAssignedAsync(id, request.AgentId, agentName);

        // Get updated ticket and notify
        var updatedTicket = await _ticketRepository.GetByIdAsync(id, ct);
        if (updatedTicket != null)
        {
            await _notificationService.NotifyTicketUpdatedAsync(updatedTicket);
        }

        return NoContent();
    }
}

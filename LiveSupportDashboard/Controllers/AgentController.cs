using System.Text;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LiveSupportDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AgentController(
    IAgentRepository agentRepository,
    ITicketRepository ticketRepository,
    IValidationService validationService,
    INotificationService notificationService)
    : ControllerBase
{
    /// <summary>
    /// Agent login - simplified authentication (production would use proper auth)
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Agent login response with token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AgentLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] AgentLoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid login request",
                Detail = "Email and password are required"
            });
        }

        var agent = await agentRepository.GetByEmailAsync(request.Email, ct);
        if (agent == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "Agent not found or invalid password"
            });
        }

        // Simplified password check - in production, use proper password hashing
        // For demo purposes, we'll accept any password for existing agents

        // Update last seen timestamp
        await agentRepository.UpdateLastSeenAsync(agent.Id, ct);

        // Generate a simple JWT-like token (in production, use proper JWT)
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{agent.Id}:{DateTime.UtcNow:O}"));
        var expiresAt = DateTime.UtcNow.AddHours(8);

        // Notify agent connected
        await notificationService.NotifyAgentConnectedAsync(agent.Name);

        return Ok(new AgentLoginResponse
        {
            AgentId = agent.Id,
            Name = agent.Name,
            Email = agent.Email,
            Token = token,
            ExpiresAt = expiresAt
        });
    }

    /// <summary>
    /// Get agent status and online information
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Agent status including online status and active tickets</returns>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(AgentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgentStatus(Guid id, CancellationToken ct = default)
    {
        var agent = await agentRepository.GetByIdAsync(id, ct);
        if (agent == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID: {id}"
            });
        }

        // Get active tickets count for this agent
        var (tickets, _) = await ticketRepository.QueryAsync(null, null, null, 1, 1000, ct);
        var activeTicketsCount =
            tickets.Count(t => t.AssignedAgentId == id && t.Status != TicketStatus.Resolved);

        // Consider agent online if last seen within 5 minutes
        var isOnline = DateTime.UtcNow - agent.UpdatedAt < TimeSpan.FromMinutes(5);

        return Ok(new AgentStatusResponse
        {
            AgentId = agent.Id,
            Name = agent.Name,
            Email = agent.Email,
            IsOnline = isOnline,
            LastSeen = agent.UpdatedAt,
            ActiveTicketsCount = activeTicketsCount
        });
    }

    /// <summary>
    /// Get all agents
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all agents</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Agent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAgents(CancellationToken ct = default)
    {
        var agents = await agentRepository.GetAllAsync(ct);
        return Ok(agents);
    }

    /// <summary>
    /// Create a ticket as an agent
    /// </summary>
    /// <param name="agentId">Agent ID creating the ticket</param>
    /// <param name="request">Ticket creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created ticket ID</returns>
    [HttpPost("{agentId:guid}/tickets")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTicketAsAgent(
        Guid agentId,
        [FromBody] CreateTicketRequest request,
        CancellationToken ct = default)
    {
        // Verify agent exists
        var agent = await agentRepository.GetByIdAsync(agentId, ct);
        if (agent == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID: {agentId}"
            });
        }

        // Validate the ticket request
        var validationResult = await validationService.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        // Create the ticket
        var ticketId = await ticketRepository.CreateAsync(request, ct);

        // Update agent's last seen
        await agentRepository.UpdateLastSeenAsync(agentId, ct);

        // Get the created ticket for notification
        var createdTicket = await ticketRepository.GetByIdAsync(ticketId, ct);
        if (createdTicket != null)
        {
            await notificationService.NotifyTicketCreatedAsync(createdTicket);
        }

        return CreatedAtAction(
            "GetTicket",
            "Tickets",
            new { id = ticketId },
            new { id = ticketId, message = "Ticket created successfully", createdBy = agent.Name });
    }

    /// <summary>
    /// Assign a ticket to an agent (can be used by agents to assign to themselves or others)
    /// </summary>
    /// <param name="agentId">Agent ID performing the assignment</param>
    /// <param name="ticketId">Ticket ID to assign</param>
    /// <param name="request">Assignment request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{agentId:guid}/assign/{ticketId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignTicketAsAgent(
        Guid agentId,
        Guid ticketId,
        [FromBody] AssignTicketRequest request,
        CancellationToken ct = default)
    {
        // Verify performing agent exists
        var performingAgent = await agentRepository.GetByIdAsync(agentId, ct);
        if (performingAgent == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID: {agentId}"
            });
        }

        // Validate the assignment request
        var validationResult = await validationService.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        // Validate business rules for assignment
        var operationValidation = await validationService.ValidateTicketOperationAsync(ticketId, "assignment", ct);
        if (!operationValidation.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in operationValidation.Errors)
            {
                problemDetails.Errors.Add(error.Property, new[] { error.Message });
            }

            return BadRequest(problemDetails);
        }

        // Perform the assignment
        var success = await ticketRepository.AssignAsync(ticketId, request.AgentId, ct);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {ticketId}"
            });
        }

        // Get assigned agent name for notification
        var assignedAgent = await agentRepository.GetByIdAsync(request.AgentId, ct);
        var assignedAgentName = assignedAgent?.Name ?? "Unknown Agent";

        // Update performing agent's last seen
        await agentRepository.UpdateLastSeenAsync(agentId, ct);

        // Notify assignment
        await notificationService.NotifyTicketAssignedAsync(ticketId, request.AgentId, assignedAgentName);

        // Get updated ticket and notify
        var updatedTicket = await ticketRepository.GetByIdAsync(ticketId, ct);
        if (updatedTicket != null)
        {
            await notificationService.NotifyTicketUpdatedAsync(updatedTicket);
        }

        return NoContent();
    }

    /// <summary>
    /// Update agent's online status (heartbeat)
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHeartbeat(Guid id, CancellationToken ct = default)
    {
        var success = await agentRepository.UpdateLastSeenAsync(id, ct);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID: {id}"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Agent logout
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout(Guid id, CancellationToken ct = default)
    {
        var agent = await agentRepository.GetByIdAsync(id, ct);
        if (agent == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID: {id}"
            });
        }

        // Notify agent disconnected
        await notificationService.NotifyAgentDisconnectedAsync(agent.Name);

        return NoContent();
    }
}

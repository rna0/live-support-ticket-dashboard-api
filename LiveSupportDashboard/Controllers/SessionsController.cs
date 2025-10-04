using System.Security.Claims;
using System.Text.Json;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSupportDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class SessionsController(
    ISessionRepository sessionRepository,
    IMessageRepository messageRepository,
    IAgentRepository agentRepository) : ControllerBase
{
    /// <summary>
    /// Create a new chat session
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request,
        CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid request",
                Detail = "UserId is required"
            });
        }

        var sessionId = await sessionRepository.CreateAsync(request, ct);
        var session = await sessionRepository.GetByIdAsync(sessionId, ct);

        if (session == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Session creation failed",
                Detail = "Session was created but could not be retrieved"
            });
        }

        var response = new SessionResponse
        {
            SessionId = session.Id,
            UserId = session.UserId,
            AgentId = session.AssignedAgentId,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt
        };

        return CreatedAtAction(nameof(GetSession), new { sessionId }, response);
    }

    /// <summary>
    /// Get session details
    /// </summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken ct = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, ct);
        if (session == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = $"No session found with ID: {sessionId}"
            });
        }

        var response = new SessionResponse
        {
            SessionId = session.Id,
            UserId = session.UserId,
            AgentId = session.AssignedAgentId,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Send a message to a session (REST fallback - prefer SignalR SendMessage)
    /// </summary>
    [HttpPost("{sessionId:guid}/messages")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(Guid sessionId, [FromBody] SendMessageRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid message",
                Detail = "Message text is required"
            });
        }

        // Check if session exists
        if (!await sessionRepository.ExistsAsync(sessionId, ct))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = $"No session found with ID: {sessionId}"
            });
        }

        // Get agent info from JWT claims
        var agentId = User.FindFirst("sub")?.Value ??
                      User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var agentName = User.FindFirst("name")?.Value ??
                        User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        if (string.IsNullOrEmpty(agentId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Authentication error",
                Detail = "Unable to determine agent identity"
            });
        }

        // Create message in database
        var messageId = await messageRepository.CreateAsync(sessionId, Guid.Parse(agentId),
            SenderType.Agent.ToString().ToLowerInvariant(), request, ct);

        // Update session activity
        await sessionRepository.UpdateLastActivityAsync(sessionId, ct);

        // Get the created message for response
        var message = await messageRepository.GetByIdAsync(messageId, ct);
        if (message == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Message creation failed",
                Detail = "Message was created but could not be retrieved"
            });
        }

        var response = new MessageResponse
        {
            MessageId = message.Id,
            SessionId = message.SessionId,
            SenderId = message.SenderId,
            SenderName = agentName,
            SenderType = message.SenderType,
            Text = message.Text,
            Attachments = string.IsNullOrEmpty(message.Attachments)
                ? null
                : JsonSerializer.Deserialize<List<MessageAttachment>>(message.Attachments),
            CreatedAt = message.CreatedAt
        };

        return CreatedAtAction(nameof(GetMessages), new { sessionId }, response);
    }

    /// <summary>
    /// Get message history for a session
    /// </summary>
    [HttpGet("{sessionId:guid}/messages")]
    [ProducesResponseType(typeof(GetMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        Guid sessionId,
        [FromQuery] Guid? afterMessageId = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (limit > 100) limit = 100;
        if (limit < 1) limit = 50;

        // Check if session exists
        if (!await sessionRepository.ExistsAsync(sessionId, ct))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = $"No session found with ID: {sessionId}"
            });
        }

        // Get messages from database
        var (messages, hasMore) = await messageRepository.GetBySessionIdAsync(sessionId, afterMessageId, limit, ct);

        // Create agent lookup dictionary for sender names
        var agentIds = messages.Where(m => m.SenderType == SenderType.Agent).Select(m => m.SenderId).Distinct()
            .ToList();
        var agentLookup = new Dictionary<Guid, string>();

        foreach (var id in agentIds)
        {
            var agent = await agentRepository.GetByIdAsync(id, ct);
            if (agent != null)
            {
                agentLookup[id] = agent.Name;
            }
        }

        var response = new GetMessagesResponse
        {
            Messages = messages.Select(m => new MessageResponse
            {
                MessageId = m.Id,
                SessionId = m.SessionId,
                SenderId = m.SenderId,
                SenderName = m.SenderType == SenderType.Agent
                    ? agentLookup.GetValueOrDefault(m.SenderId, "Unknown Agent")
                    : "User", // In production, you'd lookup user names too
                SenderType = m.SenderType,
                Text = m.Text,
                Attachments = string.IsNullOrEmpty(m.Attachments)
                    ? null
                    : JsonSerializer.Deserialize<List<MessageAttachment>>(m.Attachments),
                CreatedAt = m.CreatedAt
            }).ToList(),
            HasMore = hasMore
        };

        return Ok(response);
    }

    /// <summary>
    /// Get sessions assigned to the current agent
    /// </summary>
    [HttpGet("my-sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions(CancellationToken ct = default)
    {
        var agentId = User.FindFirst("sub")?.Value ??
                      User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(agentId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Authentication error",
                Detail = "Unable to determine agent identity"
            });
        }

        var sessions = await sessionRepository.GetByAgentIdAsync(Guid.Parse(agentId), ct);

        var response = sessions.Select(s => new SessionResponse
        {
            SessionId = s.Id,
            UserId = s.UserId,
            AgentId = s.AssignedAgentId,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            LastActivityAt = s.LastActivityAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Assign the current agent to a session
    /// </summary>
    [HttpPost("{sessionId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToSession(Guid sessionId, CancellationToken ct = default)
    {
        var agentId = User.FindFirst("sub")?.Value ??
                      User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(agentId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Authentication error",
                Detail = "Unable to determine agent identity"
            });
        }

        var success = await sessionRepository.AssignAgentAsync(sessionId, Guid.Parse(agentId), ct);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = $"No session found with ID: {sessionId}"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Close a session
    /// </summary>
    [HttpPost("{sessionId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseSession(Guid sessionId, CancellationToken ct = default)
    {
        var success =
            await sessionRepository.UpdateStatusAsync(sessionId, SessionStatus.Closed.ToString().ToLowerInvariant(),
                ct);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = $"No session found with ID: {sessionId}"
            });
        }

        return NoContent();
    }
}

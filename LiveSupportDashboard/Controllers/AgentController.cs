using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LiveSupportDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AgentController(
    IAgentRepository agentRepository,
    ITicketRepository ticketRepository,
    IValidationService validationService,
    INotificationService notificationService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration)
    : ControllerBase
{
    private TimeSpan OnlineThreshold =>
        TimeSpan.FromMinutes(configuration.GetValue<int>("Agent:OnlineThresholdMinutes"));

    /// <summary>
    /// Agent registration - creates a new agent account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Agent login response with token</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AgentLoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] AgentRegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid registration request",
                Detail = "Name, email, and password are required"
            });
        }

        if (!request.Email.Contains('@'))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid email format",
                Detail = "Please provide a valid email address"
            });
        }

        var minPasswordLength = configuration.GetValue<int>("Agent:MinPasswordLength");
        if (request.Password.Length < minPasswordLength)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Weak password",
                Detail = $"Password must be at least {minPasswordLength} characters long"
            });
        }

        var existingAgent = await agentRepository.GetByEmailAsync(request.Email, ct);
        if (existingAgent != null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Agent already exists",
                Detail = "An agent with this email address already exists"
            });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var agentId = await agentRepository.CreateAsync(request.Name, request.Email, passwordHash, ct);
        var agent = await agentRepository.GetByIdAsync(agentId, ct);

        if (agent == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Registration failed",
                Detail = "Agent was created but could not be retrieved"
            });
        }

        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenLifetimeHours = configuration.GetValue<int>("Jwt:AccessTokenLifetimeHours");
        var expiresAt = DateTime.UtcNow.AddHours(tokenLifetimeHours);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, agent.Email),
            new Claim(JwtRegisteredClaimNames.Name, agent.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = await tokenService.CreateRefreshTokenAsync(agent.Id, ct);
        var refreshTokenLifetimeDays = configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays");
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenLifetimeDays);

        await notificationService.NotifyAgentConnectedAsync(agent.Name);

        return CreatedAtAction(
            nameof(GetAgentStatus),
            new { id = agent.Id },
            new AgentLoginResponse
            {
                AgentId = agent.Id,
                Name = agent.Name,
                Email = agent.Email,
                Token = tokenString,
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            });
    }

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
        if (agent == null || !BCrypt.Net.BCrypt.Verify(request.Password, agent.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "Agent not found or invalid password"
            });
        }

        await agentRepository.UpdateLastSeenAsync(agent.Id, ct);

        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenLifetimeHours = configuration.GetValue<int>("Jwt:AccessTokenLifetimeHours");
        var expiresAt = DateTime.UtcNow.AddHours(tokenLifetimeHours);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, agent.Email),
            new Claim(JwtRegisteredClaimNames.Name, agent.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = await tokenService.CreateRefreshTokenAsync(agent.Id, ct);
        var refreshTokenLifetimeDays = configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays");
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenLifetimeDays);

        await notificationService.NotifyAgentConnectedAsync(agent.Name);

        return Ok(new AgentLoginResponse
        {
            AgentId = agent.Id,
            Name = agent.Name,
            Email = agent.Email,
            Token = tokenString,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        });
    }

    /// <summary>
    /// Get agent status and online information
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Agent status including online status and active tickets</returns>
    [HttpGet("{id:guid}/status")]
    [Authorize]
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

        var maxTicketsForActiveCount = configuration.GetValue<int>("Pagination:MaxTicketsForActiveCount");
        var (tickets, _) = await ticketRepository.QueryAsync(null, null, null, 1, maxTicketsForActiveCount, ct);
        var activeTicketsCount =
            tickets.Count(t => t.AssignedAgentId == id && t.Status != TicketStatus.Resolved);

        var isOnline = DateTime.UtcNow - agent.UpdatedAt < OnlineThreshold;

        return Ok(new AgentStatusResponse
        {
            AgentId = agent.Id,
            Name = agent.Name,
            IsOnline = isOnline,
            LastSeen = agent.UpdatedAt,
            ActiveTicketsCount = activeTicketsCount
        });
    }

    /// <summary>
    /// Get agents with optional pagination and search
    /// </summary>
    /// <param name="search">Optional search string to filter by name or email</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of agents</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(AgentsPagedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgents(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = pageSize == 20 ? configuration.GetValue<int>("Pagination:DefaultPageSize") : pageSize;

        var (agents, totalCount) = await agentRepository.QueryAsync(search, page, pageSize, ct);

        var agentResponses = agents.Select(agent => new AgentResponse
        {
            AgentId = agent.Id,
            Name = agent.Name,
            IsOnline = DateTime.UtcNow - agent.UpdatedAt < OnlineThreshold,
            LastSeen = agent.UpdatedAt
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var response = new AgentsPagedResponse
        {
            Agents = agentResponses,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a ticket as an agent
    /// </summary>
    /// <param name="agentId">Agent ID creating the ticket</param>
    /// <param name="request">Ticket creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created ticket ID</returns>
    [HttpPost("{agentId:guid}/tickets")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTicketAsAgent(
        Guid agentId,
        [FromBody] CreateTicketRequest request,
        CancellationToken ct = default)
    {
        var agent = await agentRepository.GetByIdAsync(agentId, ct);
        if (agent == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID: {agentId}"
            });
        }

        var validationResult = await validationService.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Property, [error.Message]);
            }

            return BadRequest(problemDetails);
        }

        var ticketId = await ticketRepository.CreateAsync(request, ct);

        await agentRepository.UpdateLastSeenAsync(agentId, ct);

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
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignTicketAsAgent(
        Guid agentId,
        Guid ticketId,
        [FromBody] AssignTicketRequest request,
        CancellationToken ct = default)
    {
        var performingAgent = await agentRepository.GetByIdAsync(agentId, ct);
        if (performingAgent == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID: {agentId}"
            });
        }

        var validationResult = await validationService.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Property, [error.Message]);
            }

            return BadRequest(problemDetails);
        }

        var operationValidation = await validationService.ValidateTicketOperationAsync(ticketId, "assignment", ct);
        if (!operationValidation.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in operationValidation.Errors)
            {
                problemDetails.Errors.Add(error.Property, [error.Message]);
            }

            return BadRequest(problemDetails);
        }

        var success = await ticketRepository.AssignAsync(ticketId, request.AgentId, ct);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found",
                Detail = $"No ticket found with ID: {ticketId}"
            });
        }

        var assignedAgent = await agentRepository.GetByIdAsync(request.AgentId, ct);
        var assignedAgentName = assignedAgent?.Name ?? "Unknown Agent";

        await agentRepository.UpdateLastSeenAsync(agentId, ct);

        await notificationService.NotifyTicketAssignedAsync(ticketId, request.AgentId, assignedAgentName);

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
    [Authorize]
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
    [Authorize]
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

        // Revoke all refresh tokens for this agent
        await tokenService.RevokeAllAgentTokensAsync(id, ct);

        // Notify agent disconnected
        await notificationService.NotifyAgentDisconnectedAsync(agent.Name);

        return NoContent();
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AgentLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid refresh token request",
                Detail = "Refresh token is required"
            });
        }

        // Validate the refresh token
        var isValid = await tokenService.ValidateRefreshTokenAsync(request.RefreshToken, ct);
        if (!isValid)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid refresh token",
                Detail = "The refresh token is invalid, expired, or has been revoked"
            });
        }

        // Get the refresh token details
        var refreshTokenEntity = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);
        if (refreshTokenEntity == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid refresh token",
                Detail = "The refresh token does not exist"
            });
        }

        // Get the agent
        var agent = await agentRepository.GetByIdAsync(refreshTokenEntity.AgentId, ct);
        if (agent == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = "The agent associated with this token no longer exists"
            });
        }

        // Revoke the old refresh token
        await tokenService.RevokeRefreshTokenAsync(request.RefreshToken, ct);

        // Generate new JWT access token
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenLifetimeHours = configuration.GetValue<int>("Jwt:AccessTokenLifetimeHours");
        var expiresAt = DateTime.UtcNow.AddHours(tokenLifetimeHours);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, agent.Email),
            new Claim(JwtRegisteredClaimNames.Name, agent.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Generate new refresh token
        var newRefreshToken = await tokenService.CreateRefreshTokenAsync(agent.Id, ct);
        var refreshTokenLifetimeDays = configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays");
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenLifetimeDays);

        return Ok(new AgentLoginResponse
        {
            AgentId = agent.Id,
            Name = agent.Name,
            Email = agent.Email,
            Token = tokenString,
            ExpiresAt = expiresAt,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        });
    }
}

using LiveSupportDashboard.Controllers;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace LiveSupportDashboard.Tests.Controllers;

public class TicketsControllerTests
{
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<IValidationService> _mockValidationService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly TicketsController _controller;

    public TicketsControllerTests()
    {
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockValidationService = new Mock<IValidationService>();
        _mockNotificationService = new Mock<INotificationService>();
        var mockAgentRepository = new Mock<IAgentRepository>();
        var mockConfiguration = new Mock<IConfiguration>();

        mockConfiguration.Setup(x => x["Pagination:DefaultPageSize"]).Returns("20");

        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x.Value).Returns("20");
        mockConfiguration.Setup(x => x.GetSection("Pagination:DefaultPageSize")).Returns(mockSection.Object);

        _controller = new TicketsController(
            _mockTicketRepository.Object,
            _mockValidationService.Object,
            _mockNotificationService.Object,
            mockAgentRepository.Object,
            mockConfiguration.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    #region GetTickets Tests

    /// <summary>
    /// Verifies that GetTickets returns paginated ticket list with correct headers
    /// </summary>
    [Fact]
    public async Task GetTickets_WithValidParameters_ReturnsOkWithTickets()
    {
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(), Title = "Ticket 1", Status = TicketStatus.Open, CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Ticket 2", Status = TicketStatus.InProgress, CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.QueryAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tickets, 2));
        var result = await _controller.GetTickets(null, null, null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTickets = Assert.IsAssignableFrom<IReadOnlyList<Ticket>>(okResult.Value);
        Assert.Equal(2, returnedTickets.Count);
        Assert.Equal("2", _controller.Response.Headers["X-Total-Count"]);
        Assert.Equal("1", _controller.Response.Headers["X-Page"]);
    }

    /// <summary>
    /// Verifies that query parameters are correctly passed to the repository
    /// </summary>
    [Fact]
    public async Task GetTickets_WithFilters_CallsRepositoryWithCorrectFilters()
    {
        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.QueryAsync("Open", "High", "payment", 2, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Ticket>(), 0));
        await _controller.GetTickets("Open", "High", "payment", 2, 50);
        _mockTicketRepository.Verify(
            x => x.QueryAsync("Open", "High", "payment", 2, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that invalid query parameters result in BadRequest with validation errors
    /// </summary>
    [Fact]
    public async Task GetTickets_WithInvalidParameters_ReturnsBadRequest()
    {
        var validationErrors = new[]
        {
            new ValidationError("Page", "Page must be greater than 0", "INVALID_PAGE")
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(validationErrors));
        var result = await _controller.GetTickets(null, null, null, 0, 20);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(problemDetails.Errors.ContainsKey("Page"));
    }

    /// <summary>
    /// Verifies that pageSize of 0 uses the configured default page size
    /// </summary>
    [Fact]
    public async Task GetTickets_WithPageSizeZero_UsesDefaultPageSize()
    {
        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.QueryAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Ticket>(), 0));
        await _controller.GetTickets(null, null, null, 1, 0);
        _mockTicketRepository.Verify(
            x => x.QueryAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetTicket Tests

    /// <summary>
    /// Verifies that GetTicket returns the ticket when it exists
    /// </summary>
    [Fact]
    public async Task GetTicket_WhenTicketExists_ReturnsOkWithTicket()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            Title = "Test Ticket",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        var result = await _controller.GetTicket(ticketId);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTicket = Assert.IsType<Ticket>(okResult.Value);
        Assert.Equal(ticketId, returnedTicket.Id);
        Assert.Equal("Test Ticket", returnedTicket.Title);
    }

    /// <summary>
    /// Verifies that GetTicket returns NotFound when ticket doesn't exist
    /// </summary>
    [Fact]
    public async Task GetTicket_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        var ticketId = Guid.NewGuid();

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);
        var result = await _controller.GetTicket(ticketId);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains(ticketId.ToString(), problemDetails.Detail);
    }

    #endregion

    #region CreateTicket Tests

    /// <summary>
    /// Verifies that CreateTicket returns CreatedAtAction with correct location and ticket ID
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithValidRequest_ReturnsCreated()
    {
        var request = new CreateTicketRequest
        {
            Title = "New Ticket",
            Description = "Description",
            Priority = TicketPriority.High
        };

        var createdTicketId = Guid.NewGuid();
        var createdTicket = new Ticket
        {
            Id = createdTicketId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTicketId);

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(createdTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTicket);
        var result = await _controller.CreateTicket(request);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetTicket), createdResult.ActionName);
        Assert.Equal(createdTicketId, createdResult.RouteValues!["id"]);
    }

    /// <summary>
    /// Verifies that CreateTicket sends a notification after successful creation
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithValidRequest_SendsNotification()
    {
        var request = new CreateTicketRequest
        {
            Title = "New Ticket",
            Priority = TicketPriority.Critical
        };

        var createdTicketId = Guid.NewGuid();
        var createdTicket = new Ticket
        {
            Id = createdTicketId,
            Title = request.Title,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTicketId);

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(createdTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTicket);
        await _controller.CreateTicket(request);
        _mockNotificationService.Verify(
            x => x.NotifyTicketCreatedAsync(createdTicket),
            Times.Once);
    }

    /// <summary>
    /// Verifies that CreateTicket returns BadRequest when validation fails
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new CreateTicketRequest
        {
            Title = "",
            Priority = TicketPriority.Low
        };

        var validationErrors = new[]
        {
            new ValidationError("Title", "Title is required", "TITLE_REQUIRED")
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(validationErrors));
        var result = await _controller.CreateTicket(request);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(problemDetails.Errors.ContainsKey("Title"));
    }

    /// <summary>
    /// Verifies that CreateTicket validates assigned agent when provided
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithAssignedAgent_ValidatesAgent()
    {
        var agentId = Guid.NewGuid();
        var request = new CreateTicketRequest
        {
            Title = "Assigned Ticket",
            Priority = TicketPriority.High,
            AssignedAgentId = agentId
        };

        var createdTicketId = Guid.NewGuid();

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.Is<Guid?>(g => g == agentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTicketId);

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(createdTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ticket
            {
                Id = createdTicketId,
                Title = request.Title,
                Priority = request.Priority,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        await _controller.CreateTicket(request);
        _mockValidationService.Verify(
            x => x.ValidateAsync(It.Is<Guid?>(g => g == agentId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that CreateTicket returns BadRequest when assigned agent is invalid
    /// </summary>
    [Fact]
    public async Task CreateTicket_WithInvalidAssignedAgent_ReturnsBadRequest()
    {
        var agentId = Guid.NewGuid();
        var request = new CreateTicketRequest
        {
            Title = "Assigned Ticket",
            Priority = TicketPriority.High,
            AssignedAgentId = agentId
        };

        var agentErrors = new[]
        {
            new ValidationError("AssignedAgentId", "Agent not found", "AGENT_NOT_FOUND")
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.Is<Guid?>(g => g == agentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(agentErrors));
        var result = await _controller.CreateTicket(request);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(problemDetails.Errors.ContainsKey("AssignedAgentId"));
    }

    #endregion

    #region UpdateTicketStatus Tests

    /// <summary>
    /// Verifies that UpdateTicketStatus returns NoContent when successful
    /// </summary>
    [Fact]
    public async Task UpdateTicketStatus_WithValidRequest_ReturnsNoContent()
    {
        var ticketId = Guid.NewGuid();
        var request = new UpdateTicketStatusRequest { Status = TicketStatus.InProgress };

        var existingTicket = new Ticket
        {
            Id = ticketId,
            Title = "Test Ticket",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            AssignedAgentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updatedTicket = new Ticket
        {
            Id = ticketId,
            Title = "Test Ticket",
            Status = TicketStatus.InProgress,
            Priority = TicketPriority.Medium,
            AssignedAgentId = existingTicket.AssignedAgentId,
            CreatedAt = existingTicket.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTicket);

        _mockValidationService
            .Setup(x => x.ValidateTicketOperationAsync(ticketId, "status_update", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.UpdateStatusAsync(ticketId, "InProgress", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTicket);
        var result = await _controller.UpdateTicketStatus(ticketId, request);
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Verifies that UpdateTicketStatus returns NotFound when ticket doesn't exist
    /// </summary>
    [Fact]
    public async Task UpdateTicketStatus_WhenTicketNotFound_ReturnsNotFound()
    {
        var ticketId = Guid.NewGuid();
        var request = new UpdateTicketStatusRequest { Status = TicketStatus.Resolved };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);
        var result = await _controller.UpdateTicketStatus(ticketId, request);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains(ticketId.ToString(), problemDetails.Detail);
    }

    /// <summary>
    /// Verifies that UpdateTicketStatus sends status change notification
    /// </summary>
    [Fact]
    public async Task UpdateTicketStatus_SendsStatusChangeNotification()
    {
        var ticketId = Guid.NewGuid();
        var request = new UpdateTicketStatusRequest { Status = TicketStatus.Resolved };

        var existingTicket = new Ticket
        {
            Id = ticketId,
            Title = "Test Ticket",
            Status = TicketStatus.InProgress,
            Priority = TicketPriority.High,
            AssignedAgentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTicket);

        _mockValidationService
            .Setup(x => x.ValidateTicketOperationAsync(ticketId, "status_update", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.UpdateStatusAsync(ticketId, "Resolved", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _controller.UpdateTicketStatus(ticketId, request);
        _mockNotificationService.Verify(
            x => x.NotifyTicketStatusChangedAsync(ticketId, TicketStatus.InProgress, TicketStatus.Resolved),
            Times.Once);
    }

    /// <summary>
    /// Verifies that UpdateTicketStatus returns BadRequest for invalid status values
    /// </summary>
    [Fact]
    public async Task UpdateTicketStatus_WithInvalidRequest_ReturnsBadRequest()
    {
        var ticketId = Guid.NewGuid();
        var request = new UpdateTicketStatusRequest { Status = (TicketStatus)999 };

        var validationErrors = new[]
        {
            new ValidationError("Status", "Invalid status value", "INVALID_STATUS")
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(validationErrors));

        var result = await _controller.UpdateTicketStatus(ticketId, request);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(problemDetails.Errors.ContainsKey("Status"));
    }

    /// <summary>
    /// Verifies that UpdateTicketStatus returns BadRequest when business rules are violated
    /// </summary>
    [Fact]
    public async Task UpdateTicketStatus_WithBusinessRuleViolation_ReturnsBadRequest()
    {
        var ticketId = Guid.NewGuid();
        var request = new UpdateTicketStatusRequest { Status = TicketStatus.InProgress };

        var existingTicket = new Ticket
        {
            Id = ticketId,
            Title = "Test Ticket",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var businessRuleErrors = new[]
        {
            new ValidationError("Status", "Cannot update resolved ticket", "RESOLVED_TICKET_LOCKED")
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTicket);

        _mockValidationService
            .Setup(x => x.ValidateTicketOperationAsync(ticketId, "status_update", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(businessRuleErrors));
        var result = await _controller.UpdateTicketStatus(ticketId, request);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(problemDetails.Errors.ContainsKey("Status"));
    }

    #endregion

    #region AssignTicket Tests

    /// <summary>
    /// Verifies that AssignTicket returns NoContent when successful
    /// </summary>
    [Fact]
    public async Task AssignTicket_WithValidRequest_ReturnsNoContent()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = agentId };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockValidationService
            .Setup(x => x.ValidateTicketOperationAsync(ticketId, "assignment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.AssignAsync(ticketId, agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var result = await _controller.AssignTicket(ticketId, request);
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Verifies that AssignTicket returns NotFound when ticket doesn't exist
    /// </summary>
    [Fact]
    public async Task AssignTicket_WhenTicketNotFound_ReturnsNotFound()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = agentId };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockValidationService
            .Setup(x => x.ValidateTicketOperationAsync(ticketId, "assignment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockTicketRepository
            .Setup(x => x.AssignAsync(ticketId, agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var result = await _controller.AssignTicket(ticketId, request);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains(ticketId.ToString(), problemDetails.Detail);
    }

    /// <summary>
    /// Verifies that AssignTicket returns BadRequest for invalid agent IDs
    /// </summary>
    [Fact]
    public async Task AssignTicket_WithInvalidRequest_ReturnsBadRequest()
    {
        var ticketId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = Guid.Empty };

        var validationErrors = new[]
        {
            new ValidationError("AgentId", "Invalid agent ID", "INVALID_AGENT_ID")
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(validationErrors));
        var result = await _controller.AssignTicket(ticketId, request);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(problemDetails.Errors.ContainsKey("AgentId"));
    }

    /// <summary>
    /// Verifies that AssignTicket returns BadRequest when business rules are violated
    /// </summary>
    [Fact]
    public async Task AssignTicket_WithBusinessRuleViolation_ReturnsBadRequest()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = agentId };

        var businessRuleErrors = new[]
        {
            new ValidationError("AgentId", "Agent has reached maximum tickets", "AGENT_MAX_TICKETS")
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockValidationService
            .Setup(x => x.ValidateTicketOperationAsync(ticketId, "assignment", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(businessRuleErrors));
        var result = await _controller.AssignTicket(ticketId, request);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.True(problemDetails.Errors.ContainsKey("AgentId"));
    }

    #endregion
}

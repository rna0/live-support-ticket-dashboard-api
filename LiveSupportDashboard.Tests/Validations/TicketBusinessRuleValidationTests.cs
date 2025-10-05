using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Services.Validations;

namespace LiveSupportDashboard.Tests.Validations;

/// <summary>
/// Tests for TicketBusinessRuleValidation to ensure business rules for ticket operations are properly enforced
/// </summary>
public class TicketBusinessRuleValidationTests
{
    private readonly TicketBusinessRuleValidation _validation = new();

    /// <summary>
    /// Verifies that high priority tickets require agent assignment
    /// </summary>
    [Fact]
    public async Task ValidateAsync_HighPriorityWithoutAssignment_ReturnsError()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "High priority ticket",
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            AssignedAgentId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(ticket.AssignedAgentId) &&
            e.Code == "HIGH_PRIORITY_REQUIRES_ASSIGNMENT");
    }

    /// <summary>
    /// Verifies that critical priority tickets require agent assignment
    /// </summary>
    [Fact]
    public async Task ValidateAsync_CriticalPriorityWithoutAssignment_ReturnsError()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Critical priority ticket",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.Open,
            AssignedAgentId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(ticket.AssignedAgentId) &&
            e.Code == "HIGH_PRIORITY_REQUIRES_ASSIGNMENT");
    }

    /// <summary>
    /// Verifies that high priority tickets with agent assignment pass validation
    /// </summary>
    [Fact]
    public async Task ValidateAsync_HighPriorityWithAssignment_ReturnsSuccess()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "High priority ticket",
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            AssignedAgentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that low priority tickets do not require agent assignment
    /// </summary>
    [Fact]
    public async Task ValidateAsync_LowPriorityWithoutAssignment_ReturnsSuccess()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Low priority ticket",
            Priority = TicketPriority.Low,
            Status = TicketStatus.Open,
            AssignedAgentId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that medium priority tickets do not require agent assignment
    /// </summary>
    [Fact]
    public async Task ValidateAsync_MediumPriorityWithoutAssignment_ReturnsSuccess()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Medium priority ticket",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            AssignedAgentId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that tickets in progress status require agent assignment
    /// </summary>
    [Fact]
    public async Task ValidateAsync_InProgressWithoutAssignment_ReturnsError()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "In progress ticket",
            Priority = TicketPriority.Low,
            Status = TicketStatus.InProgress,
            AssignedAgentId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(ticket.AssignedAgentId) &&
            e.Code == "INPROGRESS_REQUIRES_AGENT");
    }

    /// <summary>
    /// Verifies that tickets in progress with agent assignment pass validation
    /// </summary>
    [Fact]
    public async Task ValidateAsync_InProgressWithAssignment_ReturnsSuccess()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "In progress ticket",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.InProgress,
            AssignedAgentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that resolved tickets within 24 hours can be modified
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ResolvedTicketWithin24Hours_ReturnsSuccess()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Recently resolved ticket",
            Priority = TicketPriority.Low,
            Status = TicketStatus.Resolved,
            AssignedAgentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-12)
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that resolved tickets after 24 hours are locked from modification
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ResolvedTicketAfter24Hours_ReturnsError()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Old resolved ticket",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Resolved,
            AssignedAgentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddHours(-25)
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(ticket.Status) &&
            e.Code == "RESOLVED_TICKET_LOCKED");
    }

    /// <summary>
    /// Verifies the boundary condition for resolved ticket lock at exactly 24 hours
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ResolvedTicketExactly24Hours_ReturnsSuccess()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Ticket resolved exactly 24 hours ago",
            Priority = TicketPriority.High,
            Status = TicketStatus.Resolved,
            AssignedAgentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-23).AddMinutes(-59)
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that open tickets pass validation without special requirements
    /// </summary>
    [Fact]
    public async Task ValidateAsync_OpenTicket_ReturnsSuccess()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Open ticket",
            Priority = TicketPriority.Low,
            Status = TicketStatus.Open,
            AssignedAgentId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that multiple business rule violations are all reported
    /// </summary>
    [Fact]
    public async Task ValidateAsync_CriticalInProgressWithoutAssignment_ReturnsMultipleErrors()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Critical in progress ticket",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.InProgress,
            AssignedAgentId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _validation.ValidateAsync(ticket);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Code == "HIGH_PRIORITY_REQUIRES_ASSIGNMENT");
        Assert.Contains(result.Errors, e => e.Code == "INPROGRESS_REQUIRES_AGENT");
    }
}

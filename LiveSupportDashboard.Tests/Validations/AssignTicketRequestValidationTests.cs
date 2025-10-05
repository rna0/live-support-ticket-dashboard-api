using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Services.Interfaces;
using LiveSupportDashboard.Services.Validations;
using Moq;

namespace LiveSupportDashboard.Tests.Validations;

/// <summary>
/// Tests for AssignTicketRequestValidation to ensure agent assignment validation rules are enforced
/// </summary>
public class AssignTicketRequestValidationTests
{
    private readonly Mock<IValidation<Guid?>> _mockAgentValidator;
    private readonly AssignTicketRequestValidation _validation;

    public AssignTicketRequestValidationTests()
    {
        _mockAgentValidator = new Mock<IValidation<Guid?>>();
        _validation = new AssignTicketRequestValidation(_mockAgentValidator.Object);
    }

    /// <summary>
    /// Verifies that validation succeeds when agent ID is valid
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithValidAgentId_ReturnsSuccess()
    {
        var agentId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = agentId };

        _mockAgentValidator
            .Setup(x => x.ValidateAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var result = await _validation.ValidateAsync(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        _mockAgentValidator.Verify(x => x.ValidateAsync(agentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that validation fails when agent does not exist
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithInvalidAgentId_ReturnsError()
    {
        var agentId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = agentId };

        var agentError = new ValidationError("AgentId", "Agent does not exist", "AGENT_NOT_FOUND");
        _mockAgentValidator
            .Setup(x => x.ValidateAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[] { agentError }));

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(request.AgentId) &&
            e.Code == "AGENT_NOT_FOUND");
    }

    /// <summary>
    /// Verifies that validation fails when agent ID is empty GUID
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithEmptyGuid_ReturnsError()
    {
        var request = new AssignTicketRequest { AgentId = Guid.Empty };

        var agentError = new ValidationError("AgentId", "Agent ID cannot be empty", "AGENT_ID_EMPTY");
        _mockAgentValidator
            .Setup(x => x.ValidateAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[] { agentError }));

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(request.AgentId) &&
            e.Code == "AGENT_ID_EMPTY");
    }

    /// <summary>
    /// Verifies that all validation errors from agent validator are returned
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithMultipleAgentValidationErrors_ReturnsAllErrors()
    {
        var agentId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = agentId };

        var agentErrors = new[]
        {
            new ValidationError("AgentId", "Agent does not exist", "AGENT_NOT_FOUND"),
            new ValidationError("AgentId", "Agent is inactive", "AGENT_INACTIVE")
        };

        _mockAgentValidator
            .Setup(x => x.ValidateAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(agentErrors));

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Code == "AGENT_NOT_FOUND");
        Assert.Contains(result.Errors, e => e.Code == "AGENT_INACTIVE");
    }

    /// <summary>
    /// Verifies that agent validator is invoked with correct parameters including cancellation token
    /// </summary>
    [Fact]
    public async Task ValidateAsync_CallsAgentValidatorWithCorrectParameters()
    {
        var agentId = Guid.NewGuid();
        var request = new AssignTicketRequest { AgentId = agentId };
        var cancellationToken = CancellationToken.None;

        _mockAgentValidator
            .Setup(x => x.ValidateAsync(agentId, cancellationToken))
            .ReturnsAsync(ValidationResult.Success());

        await _validation.ValidateAsync(request, cancellationToken);

        _mockAgentValidator.Verify(
            x => x.ValidateAsync(agentId, cancellationToken),
            Times.Once);
    }
}

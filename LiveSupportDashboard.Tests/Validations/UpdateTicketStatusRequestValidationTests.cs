using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Services.Validations;

namespace LiveSupportDashboard.Tests.Validations;

/// <summary>
/// Tests for UpdateTicketStatusRequestValidation to ensure ticket status updates meet validation requirements
/// </summary>
public class UpdateTicketStatusRequestValidationTests
{
    private readonly UpdateTicketStatusRequestValidation _validation = new();

    /// <summary>
    /// Verifies that all valid ticket statuses are accepted
    /// </summary>
    [Theory]
    [InlineData(TicketStatus.Open)]
    [InlineData(TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved)]
    public async Task ValidateAsync_WithValidStatus_ReturnsSuccess(TicketStatus status)
    {
        var request = new UpdateTicketStatusRequest { Status = status };

        var result = await _validation.ValidateAsync(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Verifies that validation fails when status value is not a valid enum member
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithInvalidStatus_ReturnsError()
    {
        var request = new UpdateTicketStatusRequest { Status = (TicketStatus)999 };

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(request.Status) &&
            e.Code == "INVALID_STATUS");
    }
}

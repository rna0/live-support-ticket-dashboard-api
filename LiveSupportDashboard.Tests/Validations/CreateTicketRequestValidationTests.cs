using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Services.Validations;

namespace LiveSupportDashboard.Tests.Validations;

/// <summary>
/// Tests for CreateTicketRequestValidation to ensure ticket creation requests meet all validation requirements
/// </summary>
public class CreateTicketRequestValidationTests
{
    private readonly CreateTicketRequestValidation _validation = new();

    /// <summary>
    /// Verifies that validation succeeds when all required fields are valid
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateTicketRequest
        {
            Title = "Valid ticket title",
            Description = "Valid description",
            Priority = TicketPriority.Medium
        };

        var result = await _validation.ValidateAsync(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Verifies that validation fails when title is empty
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithEmptyTitle_ReturnsError()
    {
        var request = new CreateTicketRequest
        {
            Title = "",
            Description = "Valid description",
            Priority = TicketPriority.Low
        };

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(request.Title) &&
            e.Code == "TITLE_REQUIRED");
    }

    /// <summary>
    /// Verifies that validation fails when title is null
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithNullTitle_ReturnsError()
    {
        var request = new CreateTicketRequest
        {
            Title = null!,
            Description = "Valid description",
            Priority = TicketPriority.Low
        };

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(request.Title) &&
            e.Code == "TITLE_REQUIRED");
    }

    /// <summary>
    /// Verifies that validation fails when title exceeds 200 characters
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithTitleTooLong_ReturnsError()
    {
        var request = new CreateTicketRequest
        {
            Title = new string('x', 201),
            Description = "Valid description",
            Priority = TicketPriority.High
        };

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(request.Title) &&
            e.Code == "TITLE_TOO_LONG");
    }

    /// <summary>
    /// Verifies that validation succeeds when title is exactly 200 characters
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithTitleExactly200Characters_ReturnsSuccess()
    {
        var request = new CreateTicketRequest
        {
            Title = new string('x', 200),
            Priority = TicketPriority.Low
        };

        var result = await _validation.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that validation fails when description exceeds 4000 characters
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithDescriptionTooLong_ReturnsError()
    {
        var request = new CreateTicketRequest
        {
            Title = "Valid title",
            Description = new string('x', 4001),
            Priority = TicketPriority.Medium
        };

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(request.Description) &&
            e.Code == "DESCRIPTION_TOO_LONG");
    }

    /// <summary>
    /// Verifies that validation succeeds when description is exactly 4000 characters
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithDescriptionExactly4000Characters_ReturnsSuccess()
    {
        var request = new CreateTicketRequest
        {
            Title = "Valid title",
            Description = new string('x', 4000),
            Priority = TicketPriority.Critical
        };

        var result = await _validation.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that validation succeeds when description is null (optional field)
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithNullDescription_ReturnsSuccess()
    {
        var request = new CreateTicketRequest
        {
            Title = "Valid title",
            Description = null,
            Priority = TicketPriority.Low
        };

        var result = await _validation.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that all valid priority levels are accepted
    /// </summary>
    [Theory]
    [InlineData(TicketPriority.Low)]
    [InlineData(TicketPriority.Medium)]
    [InlineData(TicketPriority.High)]
    [InlineData(TicketPriority.Critical)]
    public async Task ValidateAsync_WithValidPriorities_ReturnsSuccess(TicketPriority priority)
    {
        var request = new CreateTicketRequest
        {
            Title = "Valid title",
            Priority = priority
        };

        var result = await _validation.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that all validation errors are returned when multiple fields are invalid
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithMultipleErrors_ReturnsAllErrors()
    {
        var request = new CreateTicketRequest
        {
            Title = "",
            Description = new string('x', 4001),
            Priority = TicketPriority.Low
        };

        var result = await _validation.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Code == "TITLE_REQUIRED");
        Assert.Contains(result.Errors, e => e.Code == "DESCRIPTION_TOO_LONG");
    }
}

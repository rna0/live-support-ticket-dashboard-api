using LiveSupportDashboard.Services.Validations;

namespace LiveSupportDashboard.Tests.Validations;

/// <summary>
/// Tests for TicketQueryValidation to ensure ticket query parameters meet validation requirements
/// </summary>
public class TicketQueryValidationTests
{
    private readonly TicketQueryValidation _validation = new();

    /// <summary>
    /// Verifies that validation succeeds with all valid query parameters
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithValidParameters_ReturnsSuccess()
    {
        var parameters = new TicketQueryParameters
        {
            Status = "Open",
            Priority = "High",
            SearchQuery = "payment issue",
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Verifies that validation fails when page number is zero or negative
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ValidateAsync_WithInvalidPage_ReturnsError(int page)
    {
        var parameters = new TicketQueryParameters
        {
            Page = page,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(parameters.Page) &&
            e.Code == "INVALID_PAGE");
    }

    /// <summary>
    /// Verifies that validation succeeds with valid page numbers
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task ValidateAsync_WithValidPage_ReturnsSuccess(int page)
    {
        var parameters = new TicketQueryParameters
        {
            Page = page,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that validation fails when page size is outside the valid range (1-100)
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(1000)]
    public async Task ValidateAsync_WithInvalidPageSize_ReturnsError(int pageSize)
    {
        var parameters = new TicketQueryParameters
        {
            Page = 1,
            PageSize = pageSize
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(parameters.PageSize) &&
            e.Code == "INVALID_PAGE_SIZE");
    }

    /// <summary>
    /// Verifies that validation succeeds with valid page sizes
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(100)]
    public async Task ValidateAsync_WithValidPageSize_ReturnsSuccess(int pageSize)
    {
        var parameters = new TicketQueryParameters
        {
            Page = 1,
            PageSize = pageSize
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that all valid ticket status filters are accepted (case-insensitive)
    /// </summary>
    [Theory]
    [InlineData("Open")]
    [InlineData("InProgress")]
    [InlineData("Resolved")]
    [InlineData("open")]
    [InlineData("OPEN")]
    public async Task ValidateAsync_WithValidStatus_ReturnsSuccess(string status)
    {
        var parameters = new TicketQueryParameters
        {
            Status = status,
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that validation fails when status filter is not a valid ticket status
    /// </summary>
    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("Closed")]
    [InlineData("Pending")]
    [InlineData("123")]
    public async Task ValidateAsync_WithInvalidStatus_ReturnsError(string status)
    {
        var parameters = new TicketQueryParameters
        {
            Status = status,
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(parameters.Status) &&
            e.Code == "INVALID_STATUS_FILTER");
    }

    /// <summary>
    /// Verifies that null status filter is accepted (no filtering)
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithNullStatus_ReturnsSuccess()
    {
        var parameters = new TicketQueryParameters
        {
            Status = null,
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that all valid priority filters are accepted (case-insensitive)
    /// </summary>
    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("Critical")]
    [InlineData("low")]
    [InlineData("CRITICAL")]
    public async Task ValidateAsync_WithValidPriority_ReturnsSuccess(string priority)
    {
        var parameters = new TicketQueryParameters
        {
            Priority = priority,
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that validation fails when priority filter is not a valid ticket priority
    /// </summary>
    [Theory]
    [InlineData("InvalidPriority")]
    [InlineData("Urgent")]
    [InlineData("Normal")]
    [InlineData("Extreme")]
    [InlineData("999")]
    [InlineData("123")]
    public async Task ValidateAsync_WithInvalidPriority_ReturnsError(string priority)
    {
        var parameters = new TicketQueryParameters
        {
            Priority = priority,
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(parameters.Priority) &&
            e.Code == "INVALID_PRIORITY_FILTER");
    }

    /// <summary>
    /// Verifies that null priority filter is accepted (no filtering)
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithNullPriority_ReturnsSuccess()
    {
        var parameters = new TicketQueryParameters
        {
            Priority = null,
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that validation succeeds with valid search query
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithValidSearchQuery_ReturnsSuccess()
    {
        var parameters = new TicketQueryParameters
        {
            SearchQuery = "payment processing issue",
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that validation fails when search query exceeds 500 characters
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithSearchQueryTooLong_ReturnsError()
    {
        var parameters = new TicketQueryParameters
        {
            SearchQuery = new string('x', 501),
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Property == nameof(parameters.SearchQuery) &&
            e.Code == "SEARCH_QUERY_TOO_LONG");
    }

    /// <summary>
    /// Verifies that validation succeeds when search query is exactly 500 characters
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithSearchQueryExactly500Characters_ReturnsSuccess()
    {
        var parameters = new TicketQueryParameters
        {
            SearchQuery = new string('x', 500),
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that null search query is accepted (no search filtering)
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithNullSearchQuery_ReturnsSuccess()
    {
        var parameters = new TicketQueryParameters
        {
            SearchQuery = null,
            Page = 1,
            PageSize = 20
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that all validation errors are returned when multiple parameters are invalid
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WithMultipleErrors_ReturnsAllErrors()
    {
        var parameters = new TicketQueryParameters
        {
            Status = "InvalidStatus",
            Priority = "InvalidPriority",
            SearchQuery = new string('x', 501),
            Page = 0,
            PageSize = 101
        };

        var result = await _validation.ValidateAsync(parameters);

        Assert.False(result.IsValid);
        Assert.Equal(5, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_PAGE");
        Assert.Contains(result.Errors, e => e.Code == "INVALID_PAGE_SIZE");
        Assert.Contains(result.Errors, e => e.Code == "INVALID_STATUS_FILTER");
        Assert.Contains(result.Errors, e => e.Code == "INVALID_PRIORITY_FILTER");
        Assert.Contains(result.Errors, e => e.Code == "SEARCH_QUERY_TOO_LONG");
    }
}

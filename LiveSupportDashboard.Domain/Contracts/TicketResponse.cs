namespace LiveSupportDashboard.Domain.Contracts;

public sealed class TicketResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedAgentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? SlaTimeLeft { get; init; } // e.g., "2h 15m"
    public List<TicketHistoryResponse> History { get; init; } = new();
}

public sealed class TicketHistoryResponse
{
    public string Id { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string Agent { get; init; } = string.Empty;
}

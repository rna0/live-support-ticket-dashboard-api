namespace LiveSupportDashboard.Domain.Contracts;

public sealed class AgentStatusResponse
{
    public Guid AgentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsOnline { get; init; }
    public DateTime LastSeen { get; init; }
    public int ActiveTicketsCount { get; init; }
}

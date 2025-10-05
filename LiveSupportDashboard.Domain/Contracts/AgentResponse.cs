namespace LiveSupportDashboard.Domain.Contracts;

public sealed class AgentResponse
{
    public Guid AgentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsOnline { get; init; }
    public DateTime LastSeen { get; init; }
}

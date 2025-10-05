namespace LiveSupportDashboard.Domain;

public sealed class RefreshToken
{
    public Guid Id { get; init; }
    public Guid AgentId { get; init; }
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public bool IsRevoked { get; init; }
}

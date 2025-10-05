namespace LiveSupportDashboard.Domain.Contracts;

public sealed class AgentLoginResponse
{
    public Guid AgentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; init; }
}

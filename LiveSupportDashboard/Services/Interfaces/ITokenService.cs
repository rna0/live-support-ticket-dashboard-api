namespace LiveSupportDashboard.Services.Interfaces;

public interface ITokenService
{
    string GenerateRefreshToken();
    Task<string> CreateRefreshTokenAsync(Guid agentId, CancellationToken ct = default);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAllAgentTokensAsync(Guid agentId, CancellationToken ct = default);
}

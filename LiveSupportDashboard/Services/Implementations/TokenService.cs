using System.Security.Cryptography;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Interfaces;

namespace LiveSupportDashboard.Services.Implementations;

public sealed class TokenService(
    IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration) : ITokenService
{
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<string> CreateRefreshTokenAsync(Guid agentId, CancellationToken ct = default)
    {
        var token = GenerateRefreshToken();

        // Refresh tokens typically last 7-30 days
        var refreshTokenLifetimeDays = configuration.GetValue("Jwt:RefreshTokenLifetimeDays", 7);
        var expiresAt = DateTime.UtcNow.AddDays(refreshTokenLifetimeDays);

        await refreshTokenRepository.CreateAsync(agentId, token, expiresAt, ct);

        return token;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        return await refreshTokenRepository.IsValidAsync(refreshToken, ct);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        await refreshTokenRepository.RevokeAsync(refreshToken, ct);
    }

    public async Task RevokeAllAgentTokensAsync(Guid agentId, CancellationToken ct = default)
    {
        await refreshTokenRepository.RevokeAllByAgentIdAsync(agentId, ct);
    }
}

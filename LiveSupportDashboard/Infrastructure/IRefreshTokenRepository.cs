using LiveSupportDashboard.Domain;

namespace LiveSupportDashboard.Infrastructure;

public interface IRefreshTokenRepository
{
    Task<Guid> CreateAsync(Guid agentId, string token, DateTime expiresAt, CancellationToken ct = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);
    Task RevokeAllByAgentIdAsync(Guid agentId, CancellationToken ct = default);
    Task<bool> IsValidAsync(string token, CancellationToken ct = default);
}

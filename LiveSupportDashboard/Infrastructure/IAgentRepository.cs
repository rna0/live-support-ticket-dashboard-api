using LiveSupportDashboard.Domain;

namespace LiveSupportDashboard.Infrastructure
{
    public interface IAgentRepository
    {
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default);

        Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken ct = default);

        Task<(IReadOnlyList<Agent> Items, int Total)> QueryAsync(
            string? search, int page, int pageSize, CancellationToken ct = default);

        Task<bool> UpdateLastSeenAsync(Guid id, CancellationToken ct = default);

        Task<Guid> CreateAsync(string name, string email, string passwordHash, CancellationToken ct = default);
    }
}

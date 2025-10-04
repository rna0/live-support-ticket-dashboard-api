using LiveSupportDashboard.Domain;

namespace LiveSupportDashboard.Infrastructure
{
    public interface IAgentRepository
    {
        /// <summary>
        /// Checks whether an agent with the provided id exists.
        /// </summary>
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Get agent by ID
        /// </summary>
        Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Get agent by email for login
        /// </summary>
        Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Get all agents
        /// </summary>
        Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Update agent's last seen timestamp for online status
        /// </summary>
        Task<bool> UpdateLastSeenAsync(Guid id, CancellationToken ct = default);
    }
}

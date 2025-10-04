using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;

namespace LiveSupportDashboard.Infrastructure
{
    public interface ISessionRepository
    {
        /// <summary>
        /// Create a new chat session
        /// </summary>
        Task<Guid> CreateAsync(CreateSessionRequest request, CancellationToken ct = default);

        /// <summary>
        /// Get session by ID
        /// </summary>
        Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Get sessions by user ID
        /// </summary>
        Task<IReadOnlyList<Session>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Get sessions assigned to agent
        /// </summary>
        Task<IReadOnlyList<Session>> GetByAgentIdAsync(Guid agentId, CancellationToken ct = default);

        /// <summary>
        /// Update session status
        /// </summary>
        Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);

        /// <summary>
        /// Assign agent to session
        /// </summary>
        Task<bool> AssignAgentAsync(Guid sessionId, Guid agentId, CancellationToken ct = default);

        /// <summary>
        /// Update last activity timestamp
        /// </summary>
        Task<bool> UpdateLastActivityAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Check if session exists
        /// </summary>
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    }
}

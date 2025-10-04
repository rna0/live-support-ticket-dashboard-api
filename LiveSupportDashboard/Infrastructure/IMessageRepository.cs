using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;

namespace LiveSupportDashboard.Infrastructure
{
    public interface IMessageRepository
    {
        /// <summary>
        /// Create a new message
        /// </summary>
        Task<Guid> CreateAsync(Guid sessionId, Guid senderId, string senderType, SendMessageRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// Get message by ID
        /// </summary>
        Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Get messages for a session with pagination
        /// </summary>
        Task<(IReadOnlyList<Message> Messages, bool HasMore)> GetBySessionIdAsync(
            Guid sessionId,
            Guid? afterMessageId = null,
            int limit = 50,
            CancellationToken ct = default);

        /// <summary>
        /// Get total message count for a session
        /// </summary>
        Task<int> GetMessageCountAsync(Guid sessionId, CancellationToken ct = default);

        /// <summary>
        /// Delete all messages for a session (when session is deleted)
        /// </summary>
        Task<bool> DeleteBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
    }
}

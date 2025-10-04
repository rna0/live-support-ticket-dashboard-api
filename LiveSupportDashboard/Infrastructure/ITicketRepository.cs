using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;

namespace LiveSupportDashboard.Infrastructure;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<Ticket> Items, int Total)> QueryAsync(
        string? status, string? priority, string? search, int page, int pageSize, CancellationToken ct = default);

    Task<Guid> CreateAsync(CreateTicketRequest req, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<bool> AssignAsync(Guid id, Guid agentId, CancellationToken ct = default);

    // Add new methods for frontend compatibility
    Task<IReadOnlyList<TicketHistory>> GetTicketHistoryAsync(Guid ticketId, CancellationToken ct = default);
    Task<TicketResponse?> GetTicketWithHistoryAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<TicketResponse> Items, int Total)> QueryWithHistoryAsync(
        string? status, string? priority, string? search, int page, int pageSize, CancellationToken ct = default);
}

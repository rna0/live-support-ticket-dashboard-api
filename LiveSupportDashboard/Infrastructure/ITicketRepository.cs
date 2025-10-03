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
}

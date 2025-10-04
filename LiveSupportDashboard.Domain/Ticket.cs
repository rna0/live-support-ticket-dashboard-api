using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Domain;

public sealed class Ticket
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TicketPriority Priority { get; init; } = TicketPriority.Low;
    public TicketStatus Status { get; init; } = TicketStatus.Open;
    public Guid? AssignedAgentId { get; init; }
    public DateTime? SlaDueAt { get; init; } // Add SLA tracking
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

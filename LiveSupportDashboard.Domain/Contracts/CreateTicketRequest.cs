using System.ComponentModel.DataAnnotations;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Domain.Contracts;

public sealed class CreateTicketRequest
{
    [Required, MaxLength(200)] public string Title { get; init; } = string.Empty;

    [MaxLength(4000)] public string? Description { get; init; }

    [Required] public TicketPriority Priority { get; init; } = TicketPriority.Low;

    public Guid? AssignedAgentId { get; init; }
}

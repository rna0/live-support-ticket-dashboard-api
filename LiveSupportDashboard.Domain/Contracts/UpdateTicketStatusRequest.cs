using System.ComponentModel.DataAnnotations;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Domain.Contracts;

public sealed class UpdateTicketStatusRequest
{
    [Required]
    public TicketStatus Status { get; init; } = TicketStatus.Open;
}

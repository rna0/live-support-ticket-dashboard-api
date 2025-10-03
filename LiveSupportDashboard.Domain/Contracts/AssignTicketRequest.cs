using System.ComponentModel.DataAnnotations;

namespace LiveSupportDashboard.Domain.Contracts;

public sealed class AssignTicketRequest
{
    [Required]
    public Guid AgentId { get; init; }
}

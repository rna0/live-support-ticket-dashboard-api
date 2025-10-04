using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifyTicketCreatedAsync(Ticket ticket);
        Task NotifyTicketUpdatedAsync(Ticket ticket);
        Task NotifyTicketStatusChangedAsync(Guid ticketId, TicketStatus oldStatus, TicketStatus newStatus);
        Task NotifyTicketAssignedAsync(Guid ticketId, Guid agentId, string agentName);
        Task NotifyAgentConnectedAsync(string agentName);
        Task NotifyAgentDisconnectedAsync(string agentName);
    }
}

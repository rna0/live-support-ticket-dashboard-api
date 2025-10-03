using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Hubs;
using LiveSupportDashboard.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LiveSupportDashboard.Services.Implementations
{
    public class SignalRNotificationService(
        IHubContext<LiveSupportHub> hubContext,
        ILogger<SignalRNotificationService> logger)
        : INotificationService
    {
        public async Task NotifyTicketCreatedAsync(Ticket ticket)
        {
            try
            {
                await hubContext.Clients.Group("SupportTeam")
                    .SendAsync("TicketCreated", ticket);

                logger.LogInformation("Notified ticket created: {TicketId}", ticket.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify ticket created: {TicketId}", ticket.Id);
            }
        }

        public async Task NotifyTicketUpdatedAsync(Ticket ticket)
        {
            try
            {
                await hubContext.Clients.Group("SupportTeam")
                    .SendAsync("TicketUpdated", ticket);

                logger.LogInformation("Notified ticket updated: {TicketId}", ticket.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify ticket updated: {TicketId}", ticket.Id);
            }
        }

        public async Task NotifyTicketStatusChangedAsync(int ticketId, TicketStatus oldStatus, TicketStatus newStatus)
        {
            try
            {
                var notification = new
                {
                    TicketId = ticketId,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = newStatus.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                await hubContext.Clients.Group("SupportTeam")
                    .SendAsync("TicketStatusChanged", notification);

                logger.LogInformation("Notified ticket status changed: {TicketId} from {OldStatus} to {NewStatus}",
                    ticketId, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify ticket status changed: {TicketId}", ticketId);
            }
        }

        public async Task NotifyTicketAssignedAsync(int ticketId, int agentId, string agentName)
        {
            try
            {
                var notification = new
                {
                    TicketId = ticketId,
                    AgentId = agentId,
                    AgentName = agentName,
                    Timestamp = DateTime.UtcNow
                };

                await hubContext.Clients.Group("SupportTeam")
                    .SendAsync("TicketAssigned", notification);

                logger.LogInformation("Notified ticket assigned: {TicketId} to agent {AgentName}",
                    ticketId, agentName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify ticket assigned: {TicketId}", ticketId);
            }
        }

        public async Task NotifyAgentConnectedAsync(string agentName)
        {
            try
            {
                await hubContext.Clients.Group("SupportTeam")
                    .SendAsync("AgentConnected", new { AgentName = agentName, Timestamp = DateTime.UtcNow });

                logger.LogInformation("Notified agent connected: {AgentName}", agentName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify agent connected: {AgentName}", agentName);
            }
        }

        public async Task NotifyAgentDisconnectedAsync(string agentName)
        {
            try
            {
                await hubContext.Clients.Group("SupportTeam")
                    .SendAsync("AgentDisconnected", new { AgentName = agentName, Timestamp = DateTime.UtcNow });

                logger.LogInformation("Notified agent disconnected: {AgentName}", agentName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify agent disconnected: {AgentName}", agentName);
            }
        }
    }
}

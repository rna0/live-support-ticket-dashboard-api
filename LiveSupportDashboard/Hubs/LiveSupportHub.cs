using Microsoft.AspNetCore.SignalR;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Hubs
{
    public class LiveSupportHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task NotifyTicketCreated(Ticket ticket)
        {
            await Clients.All.SendAsync("TicketCreated", ticket);
        }

        public async Task NotifyTicketUpdated(Ticket ticket)
        {
            await Clients.All.SendAsync("TicketUpdated", ticket);
        }

        public async Task NotifyTicketStatusChanged(int ticketId, TicketStatus oldStatus, TicketStatus newStatus)
        {
            await Clients.All.SendAsync("TicketStatusChanged", new { TicketId = ticketId, OldStatus = oldStatus, NewStatus = newStatus });
        }

        public async Task NotifyTicketAssigned(int ticketId, int agentId, string agentName)
        {
            await Clients.All.SendAsync("TicketAssigned", new { TicketId = ticketId, AgentId = agentId, AgentName = agentName });
        }

        public override async Task OnConnectedAsync()
        {
            // Join default support group
            await Groups.AddToGroupAsync(Context.ConnectionId, "SupportTeam");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SupportTeam");
            await base.OnDisconnectedAsync(exception);
        }
    }
}

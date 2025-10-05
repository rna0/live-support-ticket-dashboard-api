using System.Security.Claims;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LiveSupportDashboard.Hubs
{
    [Authorize]
    public class LiveSupportHub(ILogger<LiveSupportHub> logger) : Hub
    {
        #region Chat Session Methods

        /// <summary>
        /// Join a chat session room to receive real-time messages and events
        /// </summary>
        /// <param name="sessionId">The session ID to join</param>
        public async Task JoinRoom(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    throw new HubException("Session ID is required");
                }

                var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? Context.User?.FindFirst("sub")?.Value;
                var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                                ?? Context.User?.FindFirst("name")?.Value
                                ?? "Unknown Agent";

                if (string.IsNullOrEmpty(agentId))
                {
                    throw new HubException("Authentication required");
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

                logger.LogInformation(
                    "Agent {AgentId} ({AgentName}) joined session {SessionId} with connection {ConnectionId}",
                    agentId, agentName, sessionId, Context.ConnectionId);

                var notification = new AgentJoinedNotification
                {
                    SessionId = Guid.Parse(sessionId),
                    AgentId = Guid.Parse(agentId),
                    AgentName = agentName,
                    Timestamp = DateTime.UtcNow
                };

                await Clients.Group(sessionId).SendAsync("AgentJoined", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error joining room {SessionId}", sessionId);
                throw new HubException($"Failed to join room: {ex.Message}");
            }
        }

        /// <summary>
        /// Leave a chat session room to stop receiving events
        /// </summary>
        /// <param name="sessionId">The session ID to leave</param>
        public async Task LeaveRoom(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    throw new HubException("Session ID is required");
                }

                var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? Context.User?.FindFirst("sub")?.Value;
                var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                                ?? Context.User?.FindFirst("name")?.Value
                                ?? "Unknown Agent";

                if (string.IsNullOrEmpty(agentId))
                {
                    throw new HubException("Authentication required");
                }

                // Remove connection from session group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

                logger.LogInformation(
                    "Agent {AgentId} ({AgentName}) left session {SessionId}",
                    agentId, agentName, sessionId);

                // Notify other participants that agent left
                var notification = new AgentLeftNotification
                {
                    SessionId = Guid.Parse(sessionId),
                    AgentId = Guid.Parse(agentId),
                    AgentName = agentName,
                    Timestamp = DateTime.UtcNow
                };

                await Clients.Group(sessionId).SendAsync("AgentLeft", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error leaving room {SessionId}", sessionId);
                throw new HubException($"Failed to leave room: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a message to a chat session - broadcasts to all participants in the room
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="request">The message content and attachments</param>
        public async Task SendMessage(string sessionId, SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    throw new HubException("Session ID is required");
                }

                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    throw new HubException("Message text is required");
                }

                var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? Context.User?.FindFirst("sub")?.Value;
                var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                                ?? Context.User?.FindFirst("name")?.Value
                                ?? "Unknown Agent";

                if (string.IsNullOrEmpty(agentId))
                {
                    throw new HubException("Authentication required");
                }

                // Create message object
                var message = new ChatMessage
                {
                    MessageId = Guid.NewGuid(),
                    SessionId = Guid.Parse(sessionId),
                    SenderId = Guid.Parse(agentId),
                    SenderName = agentName,
                    SenderType = SenderType.Agent,
                    Text = request.Text,
                    Attachments = request.Attachments,
                    Timestamp = DateTime.UtcNow
                };

                logger.LogInformation(
                    "Agent {AgentId} sent message {MessageId} to session {SessionId}",
                    agentId, message.MessageId, sessionId);

                // Broadcast message to all clients in the session group
                await Clients.Group(sessionId).SendAsync("ReceiveMessage", message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message to session {SessionId}", sessionId);
                throw new HubException($"Failed to send message: {ex.Message}");
            }
        }

        /// <summary>
        /// Notify that agent is typing in a session
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="isTyping">True if typing, false if stopped</param>
        public async Task NotifyTyping(string sessionId, bool isTyping)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    throw new HubException("Session ID is required");
                }

                var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? Context.User?.FindFirst("sub")?.Value;
                var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                                ?? Context.User?.FindFirst("name")?.Value
                                ?? "Unknown Agent";

                if (string.IsNullOrEmpty(agentId))
                {
                    throw new HubException("Authentication required");
                }

                var notification = new AgentTypingNotification
                {
                    SessionId = Guid.Parse(sessionId),
                    AgentId = Guid.Parse(agentId),
                    AgentName = agentName,
                    IsTyping = isTyping
                };

                // Broadcast to others in the room (not to sender)
                await Clients.OthersInGroup(sessionId).SendAsync("AgentTyping", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending typing notification");
                throw new HubException($"Failed to send typing notification: {ex.Message}");
            }
        }

        #endregion

        #region Legacy Support Methods (kept for backward compatibility)

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        #endregion

        #region Ticket Notification Methods (Server-side only - called by NotificationService)

        public async Task NotifyTicketCreated(Ticket ticket)
        {
            await Clients.All.SendAsync("TicketCreated", ticket);
        }

        public async Task NotifyTicketUpdated(Ticket ticket)
        {
            await Clients.All.SendAsync("TicketUpdated", ticket);
        }

        public async Task NotifyTicketStatusChanged(Guid ticketId, TicketStatus oldStatus, TicketStatus newStatus)
        {
            await Clients.All.SendAsync("TicketStatusChanged",
                new { TicketId = ticketId, OldStatus = oldStatus, NewStatus = newStatus });
        }

        public async Task NotifyTicketAssigned(Guid ticketId, Guid agentId, string agentName)
        {
            await Clients.All.SendAsync("TicketAssigned",
                new { TicketId = ticketId, AgentId = agentId, AgentName = agentName });
        }

        #endregion

        #region Connection Lifecycle

        public override async Task OnConnectedAsync()
        {
            var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("sub")?.Value;
            var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                            ?? Context.User?.FindFirst("name")?.Value
                            ?? "Unknown Agent";

            logger.LogInformation(
                "Agent {AgentId} ({AgentName}) connected with connection {ConnectionId}",
                agentId, agentName, Context.ConnectionId);

            // Join default support team group for broadcast notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, "SupportTeam");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("sub")?.Value;
            var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                            ?? Context.User?.FindFirst("name")?.Value
                            ?? "Unknown Agent";

            logger.LogInformation(
                "Agent {AgentId} ({AgentName}) disconnected. Reason: {Reason}",
                agentId, agentName, exception?.Message ?? "Normal disconnection");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SupportTeam");

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Ping method for connectivity testing
        /// </summary>
        public Task<string> Ping()
        {
            return Task.FromResult($"Pong at {DateTime.UtcNow:O}");
        }

        #endregion
    }
}

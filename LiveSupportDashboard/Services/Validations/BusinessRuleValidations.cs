using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Services.Validations
{
    public class TicketBusinessRuleValidation : BaseValidation<Ticket>
    {
        protected override async Task ValidateCore(Ticket ticket, CancellationToken cancellationToken)
        {
            // Business Rule: High and Critical priority tickets must be assigned
            if ((ticket.Priority == TicketPriority.High || ticket.Priority == TicketPriority.Critical)
                && !ticket.AssignedAgentId.HasValue)
            {
                AddError(nameof(ticket.AssignedAgentId),
                    "High and Critical priority tickets must be assigned to an agent",
                    "HIGH_PRIORITY_REQUIRES_ASSIGNMENT");
            }

            switch (ticket.Status)
            {
                // Business Rule: Resolved tickets cannot be modified
                case TicketStatus.Resolved:
                {
                    var timeSinceResolution = DateTime.UtcNow - ticket.UpdatedAt;
                    if (timeSinceResolution.TotalHours < 24)
                    {
                        // Allow modifications within 24 hours of resolution
                    }
                    else
                    {
                        AddError(nameof(ticket.Status),
                            "Cannot modify tickets resolved more than 24 hours ago",
                            "RESOLVED_TICKET_LOCKED");
                    }

                    break;
                }
                // Business Rule: InProgress tickets must have an assigned agent
                case TicketStatus.InProgress when !ticket.AssignedAgentId.HasValue:
                    AddError(nameof(ticket.AssignedAgentId),
                        "In Progress tickets must have an assigned agent",
                        "INPROGRESS_REQUIRES_AGENT");
                    break;
                case TicketStatus.Open:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await Task.CompletedTask;
        }
    }

    public class TicketQueryValidation : BaseValidation<TicketQueryParameters>
    {
        protected override async Task ValidateCore(TicketQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            // Page validation
            if (parameters.Page < 1)
            {
                AddError(nameof(parameters.Page), "Page must be greater than 0", "INVALID_PAGE");
            }

            // Page size validation
            if (parameters.PageSize < 1 || parameters.PageSize > 100)
            {
                AddError(nameof(parameters.PageSize), "Page size must be between 1 and 100", "INVALID_PAGE_SIZE");
            }

            // Status validation
            if (!string.IsNullOrEmpty(parameters.Status) &&
                !Enum.TryParse<TicketStatus>(parameters.Status, ignoreCase: true, out _))
            {
                AddError(nameof(parameters.Status),
                    "Status must be one of: Open, InProgress, Resolved",
                    "INVALID_STATUS_FILTER");
            }

            // Priority validation
            if (!string.IsNullOrEmpty(parameters.Priority) &&
                !Enum.TryParse<TicketPriority>(parameters.Priority, ignoreCase: true, out _))
            {
                AddError(nameof(parameters.Priority),
                    "Priority must be one of: Low, Medium, High, Critical",
                    "INVALID_PRIORITY_FILTER");
            }

            // Search query validation
            if (!string.IsNullOrEmpty(parameters.SearchQuery) && parameters.SearchQuery.Length > 500)
            {
                AddError(nameof(parameters.SearchQuery),
                    "Search query cannot exceed 500 characters",
                    "SEARCH_QUERY_TOO_LONG");
            }

            await Task.CompletedTask;
        }
    }

    // Helper class for query parameters validation
    public class TicketQueryParameters
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? SearchQuery { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

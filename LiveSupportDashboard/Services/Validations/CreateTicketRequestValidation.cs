using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure;

namespace LiveSupportDashboard.Services.Validations
{
    public class CreateTicketRequestValidation : BaseValidation<CreateTicketRequest>
    {
        private readonly ITicketRepository _ticketRepository;

        public CreateTicketRequestValidation(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        protected override async Task ValidateCore(CreateTicketRequest request, CancellationToken cancellationToken)
        {
            // Title validation
            if (IsNullOrEmpty(request.Title))
            {
                AddError(nameof(request.Title), "Title is required", "TITLE_REQUIRED");
            }
            else if (request.Title.Length > 200)
            {
                AddError(nameof(request.Title), "Title cannot exceed 200 characters", "TITLE_TOO_LONG");
            }

            // Description validation
            if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 4000)
            {
                AddError(nameof(request.Description), "Description cannot exceed 4000 characters",
                    "DESCRIPTION_TOO_LONG");
            }

            // Priority validation
            if (!Enum.IsDefined(typeof(TicketPriority), request.Priority))
            {
                AddError(nameof(request.Priority), "Invalid priority value", "INVALID_PRIORITY");
            }

            // Agent assignment validation
            if (request.AssignedAgentId.HasValue)
            {
                if (!IsValidGuid(request.AssignedAgentId))
                {
                    AddError(nameof(request.AssignedAgentId), "Invalid agent ID format", "INVALID_AGENT_ID");
                }
                else
                {
                    // Check if agent exists (you'll need to implement this in repository or create agent service)
                    // var agentExists = await _agentRepository.ExistsAsync(request.AssignedAgentId.Value, cancellationToken);
                    // if (!agentExists)
                    // {
                    //     AddError(nameof(request.AssignedAgentId), "Agent not found", "AGENT_NOT_FOUND");
                    // }
                }
            }

            await Task.CompletedTask;
        }
    }
}

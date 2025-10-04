using LiveSupportDashboard.Domain.Contracts;

namespace LiveSupportDashboard.Services.Validations;

public class AssignTicketRequestValidation : BaseValidation<AssignTicketRequest>
{
    protected override async Task ValidateCore(AssignTicketRequest request, CancellationToken cancellationToken)
    {
        // Agent ID validation
        if (!IsValidGuid(request.AgentId))
        {
            AddError(nameof(request.AgentId), "Agent ID is required and must be a valid GUID", "INVALID_AGENT_ID");
        }

        // Future: Check if agent exists
        // var agentExists = await _agentRepository.ExistsAsync(request.AgentId, cancellationToken);
        // if (!agentExists)
        // {
        //     AddError(nameof(request.AgentId), "Agent not found", "AGENT_NOT_FOUND");
        // }

        await Task.CompletedTask;
    }
}

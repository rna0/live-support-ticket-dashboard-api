using LiveSupportDashboard.Infrastructure;

namespace LiveSupportDashboard.Services.Validations
{
    public class AgentAssignmentValidation(IAgentRepository agentRepository) : BaseValidation<Guid?>
    {
        protected override async Task ValidateCore(Guid? agentId, CancellationToken cancellationToken)
        {
            // If no agent provided, validation should be considered successful here.
            // Higher-level validators decide whether the presence is required.
            if (!agentId.HasValue) return;

            if (!IsValidGuid(agentId))
            {
                AddError(nameof(agentId), "Invalid agent ID format", "INVALID_AGENT_ID");
                return;
            }

            var exists = await agentRepository.ExistsAsync(agentId.Value, cancellationToken);
            if (!exists)
            {
                AddError(nameof(agentId), "Agent not found", "AGENT_NOT_FOUND");
            }
        }
    }
}

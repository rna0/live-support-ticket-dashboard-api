using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Services.Interfaces;

namespace LiveSupportDashboard.Services.Validations;

public class AssignTicketRequestValidation(IValidation<Guid?> agentAssignmentValidator)
    : BaseValidation<AssignTicketRequest>
{
    protected override async Task ValidateCore(AssignTicketRequest request, CancellationToken cancellationToken)
    {
        // Delegate GUID/existence checks to agent assignment validator
        var result = await agentAssignmentValidator.ValidateAsync(request.AgentId, cancellationToken);
        if (!result.IsValid)
        {
            foreach (var err in result.Errors)
            {
                AddError(nameof(request.AgentId), err.Message, err.Code);
            }
        }

        await Task.CompletedTask;
    }
}

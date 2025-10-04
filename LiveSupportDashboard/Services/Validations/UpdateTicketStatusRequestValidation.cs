using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Services.Validations
{
    public class UpdateTicketStatusRequestValidation : BaseValidation<UpdateTicketStatusRequest>
    {
        protected override async Task ValidateCore(UpdateTicketStatusRequest request,
            CancellationToken cancellationToken)
        {
            // Status validation
            if (!Enum.IsDefined(typeof(TicketStatus), request.Status))
            {
                AddError(nameof(request.Status), "Invalid status value. Must be Open, InProgress, or Resolved",
                    "INVALID_STATUS");
            }

            await Task.CompletedTask;
        }
    }
}

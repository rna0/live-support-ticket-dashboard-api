using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Services.Validations
{
    public class CreateTicketRequestValidation : BaseValidation<CreateTicketRequest>
    {
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

            await Task.CompletedTask;
        }
    }
}

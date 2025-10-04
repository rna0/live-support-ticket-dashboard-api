using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Interfaces;

namespace LiveSupportDashboard.Services.Implementations
{
    public class ValidationService(IServiceProvider serviceProvider, ITicketRepository ticketRepository)
        : IValidationService
    {
        public async Task<ValidationResult> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            var validator = serviceProvider.GetService<IValidation<T>>();

            if (validator == null)
            {
                throw new InvalidOperationException($"No validator registered for type {typeof(T).Name}");
            }

            return await validator.ValidateAsync(entity, cancellationToken);
        }

        public async Task<ValidationResult> ValidateTicketOperationAsync(Guid ticketId, string operation,
            CancellationToken cancellationToken = default)
        {
            var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);

            if (ticket == null)
            {
                return ValidationResult.Failure("TicketId", "Ticket not found", "TICKET_NOT_FOUND");
            }

            return operation.ToLowerInvariant() switch
            {
                "status_update" => await ValidateStatusUpdateOperation(ticket),
                "assignment" => await ValidateAssignmentOperation(ticket),
                _ => ValidationResult.Success()
            };
        }

        private Task<ValidationResult> ValidateStatusUpdateOperation(Ticket ticket)
        {
            var errors = new List<ValidationError>();

            // Business rule: Cannot change status of resolved tickets
            if (ticket.Status == TicketStatus.Resolved)
            {
                errors.Add(new ValidationError("Status", "Cannot modify resolved tickets", "TICKET_RESOLVED"));
            }

            return Task.FromResult(errors.Count != 0 ? ValidationResult.Failure(errors) : ValidationResult.Success());
        }

        private Task<ValidationResult> ValidateAssignmentOperation(Ticket ticket)
        {
            var errors = new List<ValidationError>();

            // Business rule: Cannot assign resolved tickets
            if (ticket.Status == TicketStatus.Resolved)
            {
                errors.Add(new ValidationError("Assignment", "Cannot assign resolved tickets", "TICKET_RESOLVED"));
            }

            return Task.FromResult(errors.Count != 0 ? ValidationResult.Failure(errors) : ValidationResult.Success());
        }
    }
}

using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Interfaces;

namespace LiveSupportDashboard.Services.Implementations
{
    public class ValidationService : IValidationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITicketRepository _ticketRepository;

        public ValidationService(IServiceProvider serviceProvider, ITicketRepository ticketRepository)
        {
            _serviceProvider = serviceProvider;
            _ticketRepository = ticketRepository;
        }

        public async Task<ValidationResult> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            var validator = _serviceProvider.GetService<IValidation<T>>();

            if (validator == null)
            {
                throw new InvalidOperationException($"No validator registered for type {typeof(T).Name}");
            }

            return await validator.ValidateAsync(entity, cancellationToken);
        }

        public async Task<ValidationResult> ValidateTicketOperationAsync(Guid ticketId, string operation,
            CancellationToken cancellationToken = default)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);

            if (ticket == null)
            {
                return ValidationResult.Failure("TicketId", "Ticket not found", "TICKET_NOT_FOUND");
            }

            return operation.ToLowerInvariant() switch
            {
                "status_update" => await ValidateStatusUpdateOperation(ticket, cancellationToken),
                "assignment" => await ValidateAssignmentOperation(ticket, cancellationToken),
                _ => ValidationResult.Success()
            };
        }

        private async Task<ValidationResult> ValidateStatusUpdateOperation(Ticket ticket,
            CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            // Business rule: Cannot change status of resolved tickets
            if (ticket.Status == TicketStatus.Resolved)
            {
                errors.Add(new ValidationError("Status", "Cannot modify resolved tickets", "TICKET_RESOLVED"));
            }

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }

        private async Task<ValidationResult> ValidateAssignmentOperation(Ticket ticket,
            CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            // Business rule: Cannot assign resolved tickets
            if (ticket.Status == TicketStatus.Resolved)
            {
                errors.Add(new ValidationError("Assignment", "Cannot assign resolved tickets", "TICKET_RESOLVED"));
            }

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }
    }
}

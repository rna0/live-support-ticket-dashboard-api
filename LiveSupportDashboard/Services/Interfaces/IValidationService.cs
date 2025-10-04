namespace LiveSupportDashboard.Services.Interfaces
{
    public interface IValidationService
    {
        Task<ValidationResult> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default);

        Task<ValidationResult> ValidateTicketOperationAsync(Guid ticketId, string operation,
            CancellationToken cancellationToken = default);
    }
}

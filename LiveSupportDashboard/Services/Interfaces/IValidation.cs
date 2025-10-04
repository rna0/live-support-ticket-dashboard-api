namespace LiveSupportDashboard.Services.Interfaces
{
    public interface IValidation<in T>
    {
        Task<ValidationResult> ValidateAsync(T entity, CancellationToken cancellationToken = default);
    }

    public class ValidationResult
    {
        public bool IsValid { get; init; }
        public List<ValidationError> Errors { get; init; } = new();

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Failure(string property, string message, string? code = null)
            => new() { IsValid = false, Errors = { new ValidationError(property, message, code) } };

        public static ValidationResult Failure(IEnumerable<ValidationError> errors)
            => new() { IsValid = false, Errors = errors.ToList() };

        public ValidationResult AddError(string property, string message, string? code = null)
        {
            Errors.Add(new ValidationError(property, message, code));
            return new ValidationResult { IsValid = false, Errors = Errors };
        }
    }

    public record ValidationError(string Property, string Message, string? Code = null);
}

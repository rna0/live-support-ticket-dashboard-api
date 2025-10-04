using System.Net.Mail;
using LiveSupportDashboard.Services.Interfaces;

namespace LiveSupportDashboard.Services.Validations
{
    public abstract class BaseValidation<T> : IValidation<T>
    {
        private readonly List<ValidationError> _errors = [];

        public virtual async Task<ValidationResult> ValidateAsync(T entity,
            CancellationToken cancellationToken = default)
        {
            _errors.Clear();
            await ValidateCore(entity, cancellationToken);

            return _errors.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.Failure(_errors);
        }

        protected abstract Task ValidateCore(T entity, CancellationToken cancellationToken);

        protected void AddError(string property, string message, string? code = null)
        {
            _errors.Add(new ValidationError(property, message, code));
        }

        protected bool IsNullOrEmpty(string? value) => string.IsNullOrWhiteSpace(value);

        protected bool IsValidGuid(Guid? guid) => guid.HasValue && guid != Guid.Empty;

        protected bool IsWithinRange(int value, int min, int max) => value >= min && value <= max;

        protected bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

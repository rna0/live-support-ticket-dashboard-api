using System.ComponentModel.DataAnnotations;

namespace LiveSupportDashboard.Domain.Contracts;

public sealed class AgentLoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;

    [Required] public string Password { get; init; } = string.Empty;
}

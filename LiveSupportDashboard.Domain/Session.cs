using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Domain;

public sealed class Session
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid? AssignedAgentId { get; init; }
    public SessionStatus Status { get; init; } = SessionStatus.Active;
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
}

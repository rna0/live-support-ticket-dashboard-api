using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Domain;

public sealed class Message
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public Guid SenderId { get; init; }
    public SenderType SenderType { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? Attachments { get; init; }
    public DateTime CreatedAt { get; init; }
}

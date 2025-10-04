using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Domain.Contracts;

public sealed record CreateSessionRequest
{
    public Guid UserId { get; init; }
    public object? Metadata { get; init; }
}

public sealed record SessionResponse
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
    public Guid? AgentId { get; init; }
    public SessionStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
}

public sealed record MessageResponse
{
    public Guid MessageId { get; init; }
    public Guid SessionId { get; init; }
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public SenderType SenderType { get; init; }
    public string Text { get; init; } = string.Empty;
    public List<MessageAttachment>? Attachments { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record GetMessagesResponse
{
    public List<MessageResponse> Messages { get; init; } = new();
    public bool HasMore { get; init; }
}

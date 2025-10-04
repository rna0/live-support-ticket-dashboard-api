using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Domain.Contracts;

public sealed record SendMessageRequest
{
    public string Text { get; init; } = string.Empty;
    public List<MessageAttachment>? Attachments { get; init; }
}

public sealed record MessageAttachment
{
    public string Url { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public long Size { get; init; }
}

public sealed record ChatMessage
{
    public Guid MessageId { get; init; }
    public Guid SessionId { get; init; }
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public SenderType SenderType { get; init; }
    public string Text { get; init; } = string.Empty;
    public List<MessageAttachment>? Attachments { get; init; }
    public DateTime Timestamp { get; init; }
}

public sealed record AgentTypingNotification
{
    public Guid SessionId { get; init; }
    public Guid AgentId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public bool IsTyping { get; init; }
}

public sealed record AgentJoinedNotification
{
    public Guid SessionId { get; init; }
    public Guid AgentId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public sealed record AgentLeftNotification
{
    public Guid SessionId { get; init; }
    public Guid AgentId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

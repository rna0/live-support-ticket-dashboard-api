using System.Text.Json.Serialization;

namespace LiveSupportDashboard.Domain.Enums;

/// <summary>
/// Represents the status of a chat session
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SessionStatus>))]
public enum SessionStatus
{
    Active,
    Closed
}

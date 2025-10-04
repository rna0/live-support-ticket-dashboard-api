using System.Text.Json.Serialization;

namespace LiveSupportDashboard.Domain.Enums;

/// <summary>
/// Represents the type of sender in a chat message
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SenderType>))]
public enum SenderType
{
    User,
    Agent
}

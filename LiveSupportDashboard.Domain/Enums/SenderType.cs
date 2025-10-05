using System.Text.Json.Serialization;

namespace LiveSupportDashboard.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<SenderType>))]
public enum SenderType
{
    User,
    Agent
}

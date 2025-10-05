using System.Text.Json.Serialization;

namespace LiveSupportDashboard.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<SessionStatus>))]
public enum SessionStatus
{
    Active,
    Closed
}

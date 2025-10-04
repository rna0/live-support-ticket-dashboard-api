namespace LiveSupportDashboard.Domain;

public sealed class TicketHistory
{
    public string Id { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string Agent { get; init; } = string.Empty;
}

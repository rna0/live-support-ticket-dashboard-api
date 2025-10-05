namespace LiveSupportDashboard.Domain.Contracts;

public sealed class AgentsPagedResponse
{
    public IReadOnlyList<AgentResponse> Agents { get; init; } = new List<AgentResponse>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

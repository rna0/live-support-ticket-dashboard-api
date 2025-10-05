using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure.Services;
using Npgsql;

namespace LiveSupportDashboard.Infrastructure;

public sealed class TicketRepository(
    NpgsqlDataSource dataSource,
    ISqlQueryLoader sqlQueryLoader,
    IConfiguration configuration)
    : ITicketRepository
{
    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Ticket", "GetById");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapTicket(reader);
    }

    public async Task<(IReadOnlyList<Ticket> Items, int Total)> QueryAsync(
        string? status, string? priority, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var maxPageSize = configuration.GetValue<int>("Pagination:MaxPageSize");
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);

        var whereConditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereConditions.Add("status = @status");
            parameters.Add(new NpgsqlParameter("status", status));
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            whereConditions.Add("priority = @priority");
            parameters.Add(new NpgsqlParameter("priority", priority));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereConditions.Add("title ILIKE @search");
            parameters.Add(new NpgsqlParameter("search", $"%{search}%"));
        }

        var whereClause = whereConditions.Count > 0
            ? $"WHERE {string.Join(" AND ", whereConditions)}"
            : string.Empty;

        var countSql = (await sqlQueryLoader.GetQueryAsync("Ticket", "GetCount")).Replace("{whereClause}", whereClause);
        var dataSql =
            (await sqlQueryLoader.GetQueryAsync("Ticket", "QueryPaginated")).Replace("{whereClause}", whereClause);

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        await using var countCmd = new NpgsqlCommand(countSql, conn);
        foreach (var param in parameters)
            countCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        await using var dataCmd = new NpgsqlCommand(dataSql, conn);
        foreach (var param in parameters)
            dataCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));

        dataCmd.Parameters.AddWithValue("limit", pageSize);
        dataCmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var items = new List<Ticket>();
        await using var reader = await dataCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(MapTicket(reader));

        return (items, total);
    }

    public async Task<Guid> CreateAsync(CreateTicketRequest req, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Ticket", "Create");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var now = DateTime.UtcNow;
        var slaDueAt = CalculateSlaDueDate(req.Priority);

        cmd.Parameters.AddWithValue("title", req.Title);
        cmd.Parameters.AddWithValue("description", (object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("priority", req.Priority.ToString());
        cmd.Parameters.AddWithValue("status", nameof(TicketStatus.Open));
        cmd.Parameters.AddWithValue("assignedAgentId", (object?)req.AssignedAgentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("slaDueAt", (object?)slaDueAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("now", now);

        var id = (Guid)(await cmd.ExecuteScalarAsync(ct))!;
        return id;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Ticket", "UpdateStatus");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected == 1;
    }

    public async Task<bool> AssignAsync(Guid id, Guid agentId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Ticket", "Assign");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("agentId", agentId);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Ticket", "Delete");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", id);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected == 1;
    }

    private static Ticket MapTicket(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        Title = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        Priority = Enum.Parse<TicketPriority>(reader.GetString(3)),
        Status = Enum.Parse<TicketStatus>(reader.GetString(4)),
        AssignedAgentId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
        SlaDueAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        CreatedAt = reader.GetDateTime(7),
        UpdatedAt = reader.GetDateTime(8)
    };

    public async Task<IReadOnlyList<TicketHistory>> GetTicketHistoryAsync(Guid ticketId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Ticket", "GetHistory");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("ticketId", ticketId);

        var history = new List<TicketHistory>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            history.Add(new TicketHistory
            {
                Id = reader.GetGuid(0).ToString(),
                Timestamp = reader.GetDateTime(5),
                Action = reader.GetString(2),
                Details = reader.GetString(3),
                Agent = reader.GetString(4)
            });
        }

        return history;
    }

    public async Task<TicketResponse?> GetTicketWithHistoryAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Ticket", "GetById");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var ticket = MapTicket(reader);
        var history = await GetTicketHistoryAsync(id, ct);

        return new TicketResponse
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority.ToString(),
            Status = ticket.Status.ToString(),
            AssignedAgentId = ticket.AssignedAgentId,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            SlaTimeLeft = CalculateSlaTimeLeft(ticket.SlaDueAt),
            History = history.Select(h => new TicketHistoryResponse
            {
                Id = h.Id,
                Timestamp = h.Timestamp,
                Action = h.Action,
                Details = h.Details,
                Agent = h.Agent
            }).ToList()
        };
    }

    public async Task<(IReadOnlyList<TicketResponse> Items, int Total)> QueryWithHistoryAsync(
        string? status, string? priority, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var (tickets, total) = await QueryAsync(status, priority, search, page, pageSize, ct);

        var ticketResponses = new List<TicketResponse>();
        foreach (var ticket in tickets)
        {
            var history = await GetTicketHistoryAsync(ticket.Id, ct);
            ticketResponses.Add(new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority.ToString(),
                Status = ticket.Status.ToString(),
                AssignedAgentId = ticket.AssignedAgentId,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                SlaTimeLeft = CalculateSlaTimeLeft(ticket.SlaDueAt),
                History = history.Select(h => new TicketHistoryResponse
                {
                    Id = h.Id,
                    Timestamp = h.Timestamp,
                    Action = h.Action,
                    Details = h.Details,
                    Agent = h.Agent
                }).ToList()
            });
        }

        return (ticketResponses, total);
    }

    private static string? CalculateSlaTimeLeft(DateTime? slaDueAt)
    {
        if (!slaDueAt.HasValue) return null;

        var timeLeft = slaDueAt.Value - DateTime.UtcNow;
        if (timeLeft.TotalMinutes < 0) return "Overdue";

        if (timeLeft.TotalDays >= 1)
            return $"{(int)timeLeft.TotalDays}d {timeLeft.Hours}h";
        else if (timeLeft.TotalHours >= 1)
            return $"{(int)timeLeft.TotalHours}h {timeLeft.Minutes}m";
        else
            return $"{timeLeft.Minutes}m";
    }

    private DateTime? CalculateSlaDueDate(TicketPriority priority)
    {
        var now = DateTime.UtcNow;
        return priority switch
        {
            TicketPriority.Critical => now.AddHours(configuration.GetValue<int>("Sla:CriticalTicketHours")),
            TicketPriority.High => now.AddHours(configuration.GetValue<int>("Sla:HighTicketHours")),
            TicketPriority.Medium => now.AddDays(configuration.GetValue<int>("Sla:MediumTicketDays")),
            TicketPriority.Low => now.AddDays(configuration.GetValue<int>("Sla:LowTicketDays")),
            _ => null
        };
    }
}

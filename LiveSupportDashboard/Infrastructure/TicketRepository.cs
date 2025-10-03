using Npgsql;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;

namespace LiveSupportDashboard.Infrastructure;

public sealed class TicketRepository(NpgsqlDataSource dataSource) : ITicketRepository
{
    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """

                                       SELECT id, title, description, priority, status, assigned_agent_id, created_at, updated_at
                                       FROM tickets
                                       WHERE id = @id
                           """;

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
        pageSize = Math.Clamp(pageSize, 1, 100);

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

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM tickets {whereClause}";

        // Get paginated data
        var dataSql = $"""

                                   SELECT id, title, description, priority, status, assigned_agent_id, created_at, updated_at
                                   FROM tickets
                                   {whereClause}
                                   ORDER BY created_at DESC
                                   LIMIT @limit OFFSET @offset
                       """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        // Execute count query
        await using var countCmd = new NpgsqlCommand(countSql, conn);
        foreach (var param in parameters)
            countCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        // Execute data query
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
        const string sql = """

                                       INSERT INTO tickets (title, description, priority, status, assigned_agent_id, created_at, updated_at)
                                       VALUES (@title, @description, @priority, @status, @assignedAgentId, @now, @now)
                                       RETURNING id
                           """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var now = DateTime.UtcNow;
        cmd.Parameters.AddWithValue("title", req.Title);
        cmd.Parameters.AddWithValue("description", (object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("priority", req.Priority.ToString());
        cmd.Parameters.AddWithValue("status", TicketStatus.Open.ToString());
        cmd.Parameters.AddWithValue("assignedAgentId", (object?)req.AssignedAgentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("now", now);

        var id = (Guid)(await cmd.ExecuteScalarAsync(ct))!;
        return id;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        const string sql = """

                                       UPDATE tickets
                                       SET status = @status, updated_at = @now
                                       WHERE id = @id
                           """;

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
        const string sql = """

                                       UPDATE tickets
                                       SET assigned_agent_id = @agentId, updated_at = @now
                                       WHERE id = @id
                           """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("agentId", agentId);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

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
        CreatedAt = reader.GetDateTime(6),
        UpdatedAt = reader.GetDateTime(7)
    };
}

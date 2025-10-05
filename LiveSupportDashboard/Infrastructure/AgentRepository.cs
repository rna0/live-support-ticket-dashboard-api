using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Infrastructure.Services;
using Npgsql;

namespace LiveSupportDashboard.Infrastructure;

public sealed class AgentRepository(
    NpgsqlDataSource dataSource,
    ISqlQueryLoader sqlQueryLoader,
    IConfiguration configuration)
    : IAgentRepository
{
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Agent", "ExistsById");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result != null;
    }

    public async Task<Agent?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Agent", "GetById");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapAgent(reader);
    }

    public async Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Agent", "GetByEmail");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("email", email);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapAgent(reader);
    }

    public async Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Agent", "GetAll");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var agents = new List<Agent>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            agents.Add(MapAgent(reader));
        }

        return agents;
    }

    public async Task<(IReadOnlyList<Agent> Items, int Total)> QueryAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var maxPageSize = configuration.GetValue<int>("Pagination:MaxPageSize");
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);

        var whereConditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereConditions.Add("(name ILIKE @search OR email ILIKE @search)");
            parameters.Add(new NpgsqlParameter("search", $"%{search}%"));
        }

        var whereClause = whereConditions.Count > 0
            ? $"WHERE {string.Join(" AND ", whereConditions)}"
            : string.Empty;

        var countSql = (await sqlQueryLoader.GetQueryAsync("Agent", "GetCount")).Replace("{whereClause}", whereClause);
        var dataSql =
            (await sqlQueryLoader.GetQueryAsync("Agent", "QueryPaginated")).Replace("{whereClause}", whereClause);

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
        dataCmd.Parameters.AddWithValue("searchTerm", search ?? string.Empty);

        var items = new List<Agent>();
        await using var reader = await dataCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(MapAgent(reader));

        return (items, total);
    }

    public async Task<bool> UpdateLastSeenAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Agent", "UpdateLastSeen");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected == 1;
    }

    public async Task<Guid> CreateAsync(string name, string email, string passwordHash, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Agent", "Create");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var agentId = Guid.NewGuid();
        cmd.Parameters.AddWithValue("id", agentId);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("password_hash", passwordHash);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(ct);
        return agentId;
    }

    private static Agent MapAgent(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        Name = reader.GetString(1),
        Email = reader.GetString(2),
        PasswordHash = reader.IsDBNull(3) ? null : reader.GetString(3),
        CreatedAt = reader.GetDateTime(4),
        UpdatedAt = reader.GetDateTime(5)
    };
}

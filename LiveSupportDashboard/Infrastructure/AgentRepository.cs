using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Infrastructure.Services;
using Npgsql;

namespace LiveSupportDashboard.Infrastructure;

public sealed class AgentRepository(NpgsqlDataSource dataSource, ISqlQueryLoader sqlQueryLoader)
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
        PasswordHash = reader.GetString(3),
        CreatedAt = reader.GetDateTime(4),
        UpdatedAt = reader.GetDateTime(5)
    };
}

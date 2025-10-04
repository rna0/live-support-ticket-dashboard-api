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

    private static Agent MapAgent(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        Name = reader.GetString(1),
        Email = reader.GetString(2),
        CreatedAt = reader.GetDateTime(3),
        UpdatedAt = reader.GetDateTime(4)
    };
}

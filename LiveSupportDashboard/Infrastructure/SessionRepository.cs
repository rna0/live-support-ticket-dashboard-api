using System.Text.Json;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure.Services;
using Npgsql;

namespace LiveSupportDashboard.Infrastructure;

public sealed class SessionRepository(NpgsqlDataSource dataSource, ISqlQueryLoader sqlQueryLoader)
    : ISessionRepository
{
    public async Task<Guid> CreateAsync(CreateSessionRequest request, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "Create");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        cmd.Parameters.AddWithValue("id", sessionId);
        cmd.Parameters.AddWithValue("user_id", request.UserId);
        cmd.Parameters.AddWithValue("assigned_agent_id", DBNull.Value);
        cmd.Parameters.AddWithValue("status", nameof(SessionStatus.Active).ToLowerInvariant());
        cmd.Parameters.AddWithValue("metadata", request.Metadata != null
            ? JsonSerializer.Serialize(request.Metadata)
            : DBNull.Value);
        cmd.Parameters.AddWithValue("now", now);

        await cmd.ExecuteNonQueryAsync(ct);
        return sessionId;
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "GetById");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapSession(reader);
    }

    public async Task<IReadOnlyList<Session>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "GetByUserId");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId);

        var sessions = new List<Session>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            sessions.Add(MapSession(reader));
        }

        return sessions;
    }

    public async Task<IReadOnlyList<Session>> GetByAgentIdAsync(Guid agentId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "GetByAgentId");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("agent_id", agentId);

        var sessions = new List<Session>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            sessions.Add(MapSession(reader));
        }

        return sessions;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "UpdateStatus");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected == 1;
    }

    public async Task<bool> AssignAgentAsync(Guid sessionId, Guid agentId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "AssignAgent");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", sessionId);
        cmd.Parameters.AddWithValue("agent_id", agentId);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected == 1;
    }

    public async Task<bool> UpdateLastActivityAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "UpdateLastActivity");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected == 1;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Session", "Exists");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result != null;
    }

    private static Session MapSession(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        UserId = reader.GetGuid(1),
        AssignedAgentId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
        Status = Enum.Parse<SessionStatus>(reader.GetString(3), ignoreCase: true),
        Metadata = reader.IsDBNull(4) ? null : reader.GetString(4),
        CreatedAt = reader.GetDateTime(5),
        UpdatedAt = reader.GetDateTime(6),
        LastActivityAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    };
}

using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Infrastructure.Services;
using Npgsql;
using NpgsqlTypes;

namespace LiveSupportDashboard.Infrastructure;

public sealed class RefreshTokenRepository(NpgsqlDataSource dataSource, ISqlQueryLoader sqlQueryLoader)
    : IRefreshTokenRepository
{
    public async Task<Guid> CreateAsync(Guid agentId, string token, DateTime expiresAt, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("RefreshToken", "Create");

        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("agentId", agentId);
        cmd.Parameters.AddWithValue("token", token);
        cmd.Parameters.Add(new NpgsqlParameter("expiresAt", NpgsqlDbType.TimestampTz) { Value = expiresAt });

        var result = await cmd.ExecuteScalarAsync(ct);
        return (Guid)result!;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("RefreshToken", "GetByToken");

        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("token", token);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new RefreshToken
        {
            Id = reader.GetGuid(0),
            AgentId = reader.GetGuid(1),
            Token = reader.GetString(2),
            ExpiresAt = reader.GetDateTime(3),
            CreatedAt = reader.GetDateTime(4),
            RevokedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            IsRevoked = reader.GetBoolean(6)
        };
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("RefreshToken", "Revoke");

        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("token", token);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RevokeAllByAgentIdAsync(Guid agentId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("RefreshToken", "RevokeAllByAgentId");

        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("agentId", agentId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> IsValidAsync(string token, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("RefreshToken", "IsValid");

        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("token", token);

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }
}

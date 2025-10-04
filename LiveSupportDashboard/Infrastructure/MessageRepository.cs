using System.Text.Json;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Domain.Enums;
using LiveSupportDashboard.Infrastructure.Services;
using Npgsql;

namespace LiveSupportDashboard.Infrastructure;

public sealed class MessageRepository(NpgsqlDataSource dataSource, ISqlQueryLoader sqlQueryLoader)
    : IMessageRepository
{
    public async Task<Guid> CreateAsync(Guid sessionId, Guid senderId, string senderType, SendMessageRequest request,
        CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Message", "Create");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var messageId = Guid.NewGuid();

        cmd.Parameters.AddWithValue("id", messageId);
        cmd.Parameters.AddWithValue("session_id", sessionId);
        cmd.Parameters.AddWithValue("sender_id", senderId);
        cmd.Parameters.AddWithValue("sender_type", senderType);
        cmd.Parameters.AddWithValue("text", request.Text);
        cmd.Parameters.AddWithValue("attachments",
            request.Attachments != null ? JsonSerializer.Serialize(request.Attachments) : DBNull.Value);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(ct);
        return messageId;
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Message", "GetById");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapMessage(reader);
    }

    public async Task<(IReadOnlyList<Message> Messages, bool HasMore)> GetBySessionIdAsync(
        Guid sessionId,
        Guid? afterMessageId = null,
        int limit = 50,
        CancellationToken ct = default)
    {
        string sql;

        if (afterMessageId.HasValue)
        {
            sql = await sqlQueryLoader.GetQueryAsync("Message", "GetBySessionIdAfter");
        }
        else
        {
            sql = await sqlQueryLoader.GetQueryAsync("Message", "GetBySessionId");
        }

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("session_id", sessionId);
        cmd.Parameters.AddWithValue("limit", limit + 1); // Get one extra to check if there are more

        if (afterMessageId.HasValue)
        {
            cmd.Parameters.AddWithValue("after_message_id", afterMessageId.Value);
        }

        var messages = new List<Message>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            messages.Add(MapMessage(reader));
        }

        var hasMore = messages.Count > limit;
        if (hasMore)
        {
            messages = messages.Take(limit).ToList();
        }

        return (messages, hasMore);
    }

    public async Task<int> GetMessageCountAsync(Guid sessionId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Message", "GetCount");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("session_id", sessionId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    public async Task<bool> DeleteBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        var sql = await sqlQueryLoader.GetQueryAsync("Message", "DeleteBySessionId");

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("session_id", sessionId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    private static Message MapMessage(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        SessionId = reader.GetGuid(1),
        SenderId = reader.GetGuid(2),
        SenderType = Enum.Parse<SenderType>(reader.GetString(3), ignoreCase: true),
        Text = reader.GetString(4),
        Attachments = reader.IsDBNull(5) ? null : reader.GetString(5),
        CreatedAt = reader.GetDateTime(6)
    };
}

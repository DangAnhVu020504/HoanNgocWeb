using Dapper;

namespace TechPos.Data.MySql;

/// <summary>Ghi W_AUDIT_LOG cho mọi thao tác ghi danh mục (ADR-06/M6 — audit từng thao tác).</summary>
public static class AuditLogger
{
    public static async Task WriteAsync(
        MySqlConnectionFactory factory,
        string userId, string username, string action,
        string tableName, string recordId,
        object? oldValue = null, object? newValue = null,
        string? ip = null, string? userAgent = null, string? correlationId = null,
        System.Data.Common.DbConnection? existingConn = null, System.Data.Common.DbTransaction? tx = null,
        CancellationToken ct = default)
    {
        const string Sql = """
            INSERT INTO W_AUDIT_LOG
            (ID, USER_ID, USERNAME, ACTION, TABLE_NAME, RECORD_ID,
             OLD_VALUE, NEW_VALUE, IP, USER_AGENT, CORRELATION_ID, CREATED_AT)
            VALUES (@id, @uid, @uname, @action, @tbl, @rid, @old, @new, @ip, @ua, @corr, @at)
            """;

        var prm = new
        {
            id = Guid.NewGuid().ToString(),
            uid = string.IsNullOrEmpty(userId) ? (string?)null : userId,
            uname = username,
            action,
            tbl = tableName,
            rid = recordId,
            old = oldValue is null ? null : System.Text.Json.JsonSerializer.Serialize(oldValue),
            @new = newValue is null ? null : System.Text.Json.JsonSerializer.Serialize(newValue),
            ip,
            ua = userAgent is { Length: > 400 } ? userAgent[..400] : userAgent,
            corr = correlationId,
            at = DateTime.UtcNow
        };

        if (existingConn is not null)
        {
            await existingConn.ExecuteAsync(new CommandDefinition(Sql, prm, tx, cancellationToken: ct));
            return;
        }
        await using var conn = await factory.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(Sql, prm, cancellationToken: ct));
    }
}

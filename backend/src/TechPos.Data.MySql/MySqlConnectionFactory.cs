using MySqlConnector;

namespace TechPos.Data.MySql;

/// <summary>
/// Factory tạo connection MySQL 8 cho mirror + W_* tables.
/// Connection string cấu hình qua appsettings/env (không hard-code secret).
/// </summary>
public sealed class MySqlConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public MySqlConnection Create() => new(_connectionString);

    public async Task<MySqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>Cài đặt hệ thống lưu W_APP_SETTING (key/value, web-only).</summary>
public sealed class SettingsRepository
{
    private readonly MySqlConnectionFactory _factory;
    public SettingsRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<AppSettingDto>(
            "SELECT KEY_ AS `Key`, VALUE_ AS Value, UPDATED_AT AS UpdatedAt FROM W_APP_SETTING ORDER BY KEY_"))
            .ToList();
    }

    public async Task<Dictionary<string, string?>> GetDictionaryAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<(string Key, string? Value)>(
            "SELECT KEY_, VALUE_ FROM W_APP_SETTING");
        return rows.ToDictionary(r => r.Key, r => r.Value);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT VALUE_ FROM W_APP_SETTING WHERE KEY_ = @k", new { k = key });
    }

    public async Task SaveAsync(IReadOnlyDictionary<string, string?> settings, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        foreach (var (key, value) in settings)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO W_APP_SETTING (KEY_, VALUE_, UPDATED_AT) VALUES (@k, @v, @now)
                ON DUPLICATE KEY UPDATE VALUE_ = @v, UPDATED_AT = @now
                """, new { k = key, v = value, now = DateTime.UtcNow }, tx);
        }
        await tx.CommitAsync(ct);
    }
}

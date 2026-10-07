using Dapper;

namespace TechPos.Data.MySql;

/// <summary>
/// Migration runner idempotent: chạy các file .sql trong database/migrations theo thứ tự tên,
/// ghi lại vào W_MIGRATION. Chạy khi API / Sync Service khởi động.
/// </summary>
public sealed class MigrationRunner
{
    private readonly MySqlConnectionFactory _factory;
    private readonly string _migrationsFolder;
    private readonly Serilog.ILogger _logger;

    public MigrationRunner(MySqlConnectionFactory factory, string migrationsFolder, Serilog.ILogger? logger = null)
    {
        _factory = factory;
        _migrationsFolder = migrationsFolder;
        _logger = logger ?? Serilog.Log.ForContext<MigrationRunner>();
    }

    public async Task RunAllAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_migrationsFolder);
        var files = Directory.GetFiles(_migrationsFolder, "*.sql").Order().ToArray();

        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS W_MIGRATION (
              NAME VARCHAR(128) NOT NULL PRIMARY KEY,
              APPLIED_AT DATETIME(3) NOT NULL
            ) ENGINE=InnoDB;
            """, ct);

        var applied = (await conn.QueryAsync<string>(
            "SELECT NAME FROM W_MIGRATION")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            if (applied.Contains(name)) continue;

            var sql = await File.ReadAllTextAsync(file, ct);
            // Chạy từng statement: MySqlConnector không hỗ trợ multi-statement mặc định
            var statements = SplitStatements(sql);
            foreach (var stmt in statements)
            {
                if (string.IsNullOrWhiteSpace(stmt)) continue;
                await conn.ExecuteAsync(stmt);
            }
            await conn.ExecuteAsync("INSERT INTO W_MIGRATION (NAME, APPLIED_AT) VALUES (@Name, @At)",
                new { Name = name, At = DateTime.UtcNow });
            _logger.Information("Migration applied: {Migration}", name);
        }
    }

    private static IEnumerable<string> SplitStatements(string sql)
    {
        // Tách theo dấu ';' ở cuối dòng — đủ dùng cho DDL đơn giản của migration.
        var sb = new System.Text.StringBuilder();
        foreach (var line in sql.Split('\n'))
        {
            sb.AppendLine(line);
            if (line.TrimEnd().EndsWith(';'))
            {
                yield return sb.ToString().TrimEnd(';');
                sb.Clear();
            }
        }
        var tail = sb.ToString();
        if (!string.IsNullOrWhiteSpace(tail)) yield return tail;
    }
}
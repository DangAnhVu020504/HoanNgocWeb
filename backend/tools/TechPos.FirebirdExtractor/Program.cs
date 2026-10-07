using System.Text;
using System.Text.Json;
using TechPos.Data.Firebird;
// ============================================================
// TechPos.FirebirdExtractor — Phase 0 discovery (mục 4)
// Usage: TechPos.FirebirdExtractor <path-to-fdb> [output-dir] [clientLibraryDir]
// KHÔNG sửa DB — connection READ-ONLY.
// Output: discovery/tables/<TABLE>.json + SCHEMA_DISCOVERY.md
// ============================================================

// ============================================================
// TechPos.FirebirdExtractor — Phase 0 discovery (mục 4)
// Usage:
//   TechPos.FirebirdExtractor <path-to-fdb> [output-dir] [clientLibraryDir]   → full discovery
//   TechPos.FirebirdExtractor --sql <path-to-fdb> "<sql>" [clientLibraryDir]  → chạy 1 query READ-ONLY (đối chiếu LOAI/MODE...)
// KHÔNG sửa DB.
// ============================================================

if (args.Length >= 3 && args[0] == "--sql")
{
    var dbPathQ = args[1];
    var sql = args[2];
    var clientLibQ = args.Length > 3 ? args[3] : "";
    try
    {
        var factoryQ = new FirebirdConnectionFactory(new(dbPathQ, clientLibQ, "SYSDBA", "masterkey"));
        await using var conn = await factoryQ.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var r = await cmd.ExecuteReaderAsync();
        var header = string.Join(" | ", Enumerable.Range(0, r.FieldCount).Select(r.GetName));
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));
        while (await r.ReadAsync())
        {
            var vals = new string[r.FieldCount];
            for (int i = 0; i < r.FieldCount; i++)
                vals[i] = r.IsDBNull(i) ? "" : Convert.ToString(r.GetValue(i)) ?? "";
            Console.WriteLine(string.Join(" | ", vals));
        }
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
}

if (args.Length < 1)
{
    Console.WriteLine("Usage: TechPos.FirebirdExtractor <path-to-fdb> [output-dir] [clientLibraryDir]");
    return 1;
}

var dbPath = args[0];
var outputDir = args.Length > 1 ? args[1] : "discovery";
var clientLib = args.Length > 2 ? args[2] : "";

Console.WriteLine($"DB: {dbPath}");
try
{
    var factory = new FirebirdConnectionFactory(new(dbPath, clientLib, "SYSDBA", "masterkey"));
    var discovery = new FirebirdSchemaDiscovery(factory);

    var tables = await discovery.DiscoverAllAsync();
    var generators = await discovery.DiscoverGeneratorsAsync();

// JSON per table
var jsonDir = Path.Combine(outputDir, "tables");
Directory.CreateDirectory(jsonDir);
foreach (var t in tables)
{
    var json = JsonSerializer.Serialize(t, new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
    await File.WriteAllTextAsync(Path.Combine(jsonDir, $"{t.Name}.json"), json);
}

// Markdown summary
var md = new StringBuilder();
md.AppendLine("# SCHEMA DISCOVERY — Firebird thật (Phase 0)");
md.AppendLine($"> Nguồn: `{dbPath}` · Sinh lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
md.AppendLine($"> Tổng bảng/view: **{tables.Count}** (views: {tables.Count(t => t.IsView)}) · Generators: {generators.Count}");
md.AppendLine();
md.AppendLine("| # | Bảng | Loại | Số cột | PK | Index | Trigger |");
md.AppendLine("|---|---|---|---|---|---|---|");
int i = 0;
foreach (var t in tables)
{
    md.AppendLine($"| {++i} | {t.Name} | {(t.IsView ? "VIEW" : "TABLE")} | {t.Columns.Count} | " +
                  $"{string.Join(",", t.PrimaryKeys)} | {t.Indexes.Count} | {t.Triggers.Count} |");
}
md.AppendLine();
md.AppendLine("## Bảng có TIMEMODIFIED (đủ điều kiện watermark)");
foreach (var t in tables.Where(t => t.Columns.Any(c => c.Name.Equals("TIMEMODIFIED", StringComparison.OrdinalIgnoreCase))))
    md.AppendLine($"- {t.Name} ({t.Columns.Count} cột)");
await File.WriteAllTextAsync(Path.Combine(outputDir, "SCHEMA_DISCOVERY.md"), md.ToString());

Console.WriteLine($"Discovered {tables.Count} relations → {Path.GetFullPath(outputDir)}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.ToString());
    try { await File.WriteAllTextAsync("extractor-error.txt", ex.ToString()); } catch { }
    return 2;
}
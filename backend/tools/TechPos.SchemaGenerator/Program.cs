using System.Text;
using System.Text.Json;

// ============================================================
// TechPos.SchemaGenerator — Phase 0→1: discovery JSON → MySQL mirror DDL
// Usage: TechPos.SchemaGenerator <discovery-tables-dir> [output-ddl-path]
// Quy tắc (mục 5): giữ tên bảng/cột UPPERCASE, ID CHAR(36), thêm cột W_ sync,
// DECIMAL cho tiền, utf8mb4. KHÔNG tự suy FK — chỉ index trên …ID.
// ============================================================

if (args.Length < 1)
{
    Console.WriteLine("Usage: TechPos.SchemaGenerator <discovery-tables-dir> [output-ddl-path]");
    return 1;
}

var dir = args[0];
var tables = new List<DiscoveredTableFile>();

foreach (var f in Directory.GetFiles(dir, "*.json"))
{
    var t = JsonSerializer.Deserialize<DiscoveredTableFile>(await File.ReadAllTextAsync(f));
    if (t is not null && !t.IsView) tables.Add(t);
}

var sb = new StringBuilder();
sb.AppendLine("-- AUTO-GENERATED từ Firebird discovery — KHÔNG sửa tay. Nguồn: tools/TechPos.SchemaGenerator");
sb.AppendLine("-- Sinh lúc: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
sb.AppendLine();

foreach (var t in tables.OrderBy(x => x.Name))
    EmitTable(sb, t);

var ddlPath = args.Length > 1 ? args[1] : "mirror_ddl.sql";
await File.WriteAllTextAsync(ddlPath, sb.ToString());
Console.WriteLine($"DDL written: {Path.GetFullPath(ddlPath)} ({tables.Count} tables)");
return 0;

static void EmitTable(StringBuilder sb, DiscoveredTableFile t)
{
    var lines = new List<string>();
    foreach (var c in t.Columns.OrderBy(c => c.Position))
    {
        lines.Add($"  `{c.Name}` {MapType(c)}{(c.Nullable ? " NULL" : " NOT NULL")}" +
                  (c.DefaultSource is null ? "" : $" /* default FB: {c.DefaultSource} */"));
    }
    // Cột sync web (mục 5)
    lines.Add("  `W_SYNC_AT` DATETIME(3) NULL");
    lines.Add("  `W_SYNC_REV` BIGINT NOT NULL DEFAULT 0");
    lines.Add("  `W_ORIGIN` VARCHAR(8) NOT NULL DEFAULT 'FB'");
    lines.Add("  `W_ROW_HASH` CHAR(32) NULL");
    lines.Add("  `W_DELETED` TINYINT(1) NOT NULL DEFAULT 0");
    lines.Add("  `W_DELETED_AT` DATETIME(3) NULL");

    var pk = string.Join(", ", t.PrimaryKeys.Select(p => $"`{p}`"));
    if (!string.IsNullOrEmpty(pk))
        lines.Add($"  PRIMARY KEY ({pk})");

    if (t.Columns.Any(c => c.Name.Equals("TIMEMODIFIED", StringComparison.OrdinalIgnoreCase)))
        lines.Add("  KEY `IX_SYNC_WM` (`TIMEMODIFIED`, `ID`)");

    foreach (var fkCol in t.Columns.Where(c => c.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase)
             && c.Name.Length > 2 && !t.PrimaryKeys.Contains(c.Name)).Select(c => c.Name).Distinct())
        lines.Add($"  KEY `IX_{t.Name}_{fkCol}` (`{fkCol}`)");

    sb.AppendLine($"CREATE TABLE IF NOT EXISTS `{t.Name}` (");
    sb.AppendLine(string.Join(",\n", lines));
    sb.AppendLine(") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;");
    sb.AppendLine();
}

static string MapType(DiscoveredTableFile.Col c)
{
    // FIELD_LENGTH của Firebird là số BYTE (charset UTF8 ⇒ ×4) → quy về số ký tự
    var charLen = Math.Max(1, c.Length / 4);
    return c.FieldType switch
    {
        37 => charLen > 16000 ? "TEXT" : $"VARCHAR({charLen})",        // VARYING
        14 => charLen > 255 ? "TEXT" : $"CHAR({charLen})",             // TEXT (char)
        7  => (c.SubType == 1 || c.SubType == 2) ? $"DECIMAL(4,{-c.Scale})" : "SMALLINT",
        8  => (c.SubType == 1 || c.SubType == 2) ? $"DECIMAL(9,{-c.Scale})" : "INT",
        9  => (c.SubType == 1 || c.SubType == 2)                       // QUAD/INT64 numeric
                ? $"DECIMAL({Math.Clamp(c.Precision <= 0 ? 18 : c.Precision, 1, 38)},{Math.Clamp(-c.Scale, 0, 6)})"
                : "BIGINT",
        16 => (c.SubType == 1 || c.SubType == 2)                       // INT64 BIGINT / numeric 128
                ? $"DECIMAL({Math.Clamp(c.Precision <= 0 ? 18 : c.Precision, 1, 38)},{Math.Clamp(-c.Scale, 0, 6)})"
                : "BIGINT",
        10 => "FLOAT",
        27 => "DOUBLE",
        35 => "DATETIME(3)",                                           // TIMESTAMP
        12 => "DATE",
        13 => "TIME",
        261 => c.SubType == 1 ? "LONGTEXT" : "LONGBLOB",               // BLOB text vs binary
        _ => $"TEXT /* unknown FB type {c.FieldType} sub {c.SubType} */"
    };
}

public sealed class DiscoveredTableFile
{
    public string Name { get; set; } = "";
    public bool IsView { get; set; }
    public List<Col> Columns { get; set; } = [];
    public List<string> PrimaryKeys { get; set; } = [];
    public List<string> Triggers { get; set; } = [];

    public sealed class Col
    {
        public string Name { get; set; } = "";
        public short FieldType { get; set; }
        public short SubType { get; set; }
        public int Length { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool Nullable { get; set; }
        public string? DefaultSource { get; set; }
        public int Position { get; set; }
    }
}
using System.Text;
using Dapper;
using MySqlConnector;

// ============================================================
// TechPos.DataCompare — Parity Firebird ↔ MySQL (mục 39)
// Usage: TechPos.DataCompare <fdb-path> <mysql-conn-string> [clientLibraryDir]
// So sánh: COUNT từng bảng chung, phát hiện thiếu/dư. Checksum chi tiết ở Phase 8.
// ============================================================

if (args.Length < 2)
{
    Console.WriteLine("Usage: TechPos.DataCompare <fdb-path> <mysql-conn-string> [clientLibraryDir]");
    return 1;
}

var dbPath = args[0];
var myCs = args[1];
var clientLib = args.Length > 2 ? args[2] : "";

var fbCsb = new FirebirdSql.Data.FirebirdClient.FbConnectionStringBuilder
{
    Database = dbPath, UserID = "SYSDBA", Password = "masterkey",
    ServerType = FirebirdSql.Data.FirebirdClient.FbServerType.Embedded, Dialect = 3, Charset = "UTF8"
};
if (clientLib.Length > 0) fbCsb.ClientLibrary = clientLib;

await using var fb = new FirebirdSql.Data.FirebirdClient.FbConnection(fbCsb.ToString());
await fb.OpenAsync();

var fbTables = (await fb.QueryAsync<string>(
    "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NULL"))
    .Select(n => n.TrimEnd('\0', ' ')).ToList();

await using var my = new MySqlConnection(myCs);
await my.OpenAsync();
var myTables = (await my.QueryAsync<string>(
    "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'"))
    .Where(n => !n.StartsWith("W_", StringComparison.OrdinalIgnoreCase)).ToList();

var mismatches = 0;
Console.WriteLine($"{"Bảng",-32} {"Firebird",10} {"MySQL",10} Trạng thái");
foreach (var t in fbTables.OrderBy(x => x).Where(x => myTables.Contains(x, StringComparer.OrdinalIgnoreCase)))
{
    var fbCount = await fb.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM \"{t}\"");
    var myCount = await my.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM `{t}`");
    var ok = fbCount == myCount;
    if (!ok) mismatches++;
    Console.WriteLine($"{t,-32} {fbCount,10:N0} {myCount,10:N0} {(ok ? "OK" : "LECH")}");
}
Console.WriteLine($"\nTổng lệch: {mismatches}. Mục tiêu parity: 0 (mục 39).");
return mismatches == 0 ? 0 : 2;
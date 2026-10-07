using Dapper;
using MySqlConnector;

// ============================================================
// TechPos.AdminSeed — tạo/cập nhật tài khoản web đầu tiên (mục 34)
// Usage: TechPos.AdminSeed <mysql-conn> <username> <password> [roleCode=SUPER_ADMIN]
// Ví dụ: TechPos.AdminSeed "server=localhost;database=techposweb;user id=root;password=xxx" admin Admin@123
// Không ghi gì vào Firebird.
// ============================================================

if (args.Length < 3)
{
    Console.WriteLine("Usage: TechPos.AdminSeed <mysql-conn> <username> <password> [roleCode]");
    return 1;
}

var cs = args[0];
var username = args[1];
var password = args[2];
var roleCode = args.Length > 3 ? args[3] : "SUPER_ADMIN";

if (password.Length < 8)
{
    Console.Error.WriteLine("Mật khẩu tối thiểu 8 ký tự.");
    return 2;
}

await using var conn = new MySqlConnection(cs);
await conn.OpenAsync();

// Đọc scalar qua ADO thuần, chuẩn hoá kiểu trả về (cột CHAR(36) có thể về string/byte[]/Guid tuỳ provider)
string? roleId;
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT CAST(ID AS CHAR(36)) FROM W_ROLE WHERE CODE = @c LIMIT 1";
    cmd.Parameters.Add(new MySqlParameter("@c", roleCode));
    var raw = await cmd.ExecuteScalarAsync();
    roleId = raw as string ?? (raw is byte[] b ? System.Text.Encoding.ASCII.GetString(b) : raw?.ToString());
}

if (string.IsNullOrEmpty(roleId))
{
    Console.Error.WriteLine($"Không tìm thấy role '{roleCode}'. Chạy database/seed/001_roles_permissions_flags.sql trước.");
    return 3;
}

var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
var userId = Guid.NewGuid().ToString();

string? existingId;
await using (var cmd2 = conn.CreateCommand())
{
    cmd2.CommandText = "SELECT CAST(ID AS CHAR(36)) FROM W_USER WHERE USERNAME = @u LIMIT 1";
    cmd2.Parameters.Add(new MySqlParameter("@u", username));
    var raw = await cmd2.ExecuteScalarAsync();
    existingId = raw as string ?? (raw is byte[] b ? System.Text.Encoding.ASCII.GetString(b) : raw?.ToString());
}
if (existingId is null)
{
    await conn.ExecuteAsync("""
        INSERT INTO W_USER (ID, USERNAME, PASSWORD_HASH, FULL_NAME, IS_ACTIVE, MUST_CHANGE_PASSWORD, TIMECREATED)
        VALUES (@id, @u, @h, @name, 1, 0, NOW(3))
        """, new { id = userId, u = username, h = hash, name = username });
}
else
{
    userId = existingId;
    await conn.ExecuteAsync("""
        UPDATE W_USER SET PASSWORD_HASH = @h, IS_ACTIVE = 1, FAILED_LOGIN_COUNT = 0, LOCKED_UNTIL = NULL, TIMEMODIFIED = NOW(3)
        WHERE ID = @id
        """, new { id = userId, h = hash });
}

await conn.ExecuteAsync("INSERT IGNORE INTO W_USER_ROLE (USER_ID, ROLE_ID) VALUES (@u, @r)",
    new { u = userId, r = roleId });
await conn.ExecuteAsync("INSERT IGNORE INTO W_USER_SCOPE (USER_ID, SCOPE_ENUM, SCOPE_ID) VALUES (@u, 'ALL', '')",
    new { u = userId });

Console.WriteLine($"OK: user '{username}' (role {roleCode}) đã sẵn sàng đăng nhập web.");
return 0;
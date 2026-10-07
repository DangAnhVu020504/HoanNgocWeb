using FirebirdSql.Data.FirebirdClient;

namespace TechPos.Data.Firebird;

/// <summary>
/// Connection Firebird 2.5 Embedded READ-ONLY (mục 53/58: không ghi vào Firebird).
/// Phải chạy cùng máy với file FDB; ClientLibrary trỏ tới fbembed.dll + ICU 3.0 của app cũ.
/// </summary>
public sealed class FirebirdConnectionFactory
{
    private readonly DatabaseOptionsLike _options;

    public record DatabaseOptionsLike(
        string DatabasePath, string ClientLibrary, string User, string Password);

    public FirebirdConnectionFactory(DatabaseOptionsLike options) => _options = options;

    public FbConnection Create()
    {
        var csb = new FbConnectionStringBuilder
        {
            Database = _options.DatabasePath,
            UserID = _options.User,
            Password = _options.Password,
            ServerType = FbServerType.Embedded,
            Dialect = 3,
            Charset = "UTF8"
        };
        if (!string.IsNullOrWhiteSpace(_options.ClientLibrary))
            csb.ClientLibrary = _options.ClientLibrary;
        return new FbConnection(csb.ToString());
    }

    public async Task<FbConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = Create();
        await conn.OpenAsync(ct);
        return conn;
    }
}
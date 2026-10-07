namespace TechPos.Shared;

public sealed class DatabaseOptions
{
    public const string Section = "Database";
    public string MySqlConnectionString { get; set; } = string.Empty;
    public string FirebirdDatabasePath { get; set; } = string.Empty;
    public string FirebirdClientLibrary { get; set; } = string.Empty; // thư mục chứa fbembed.dll + ICU 3.0
    public string FirebirdUser { get; set; } = "SYSDBA";
    public string FirebirdPassword { get; set; } = "masterkey";
}

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "TechPos.Api";
    public string Audience { get; set; } = "TechPos.Web";
    public string SecretKey { get; set; } = string.Empty; // env variable, không commit secret thật
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class FeatureFlagOptions
{
    public const string Section = "FeatureFlags";
    public bool EnableWebTransaction { get; set; } = false;   // GĐ 1: web READ chứng từ FB
    public bool EnableTwoWaySync { get; set; } = false;       // mặc định false (mục 50)
    public bool EnableEmail { get; set; } = true;
    public bool EnableSignalR { get; set; } = true;
    public bool EnableCod { get; set; } = true;
    public bool EnableProduction { get; set; } = false;
    public bool EnableAdvancedReport { get; set; } = false;
    public bool EnableCatalogWrite { get; set; } = false; // Phase 4 CRUD danh mục (mặc định OFF)
}

public static class SyncTables
{
    /// <summary>Nhóm nghiệp vụ — sync 30 giây (mục 8).</summary>
    public static readonly string[] Transaction =
    [
        "TDONHANG", "TDONHANGCHITIET", "TTHUCHI", "TCHITIETTHANHTOAN",
        "TDATHANGNCC", "TDATHANGNCCCHITIET", "TGUIHANGCOD", "TKETQUAGUICOD",
        "TBANGLUONG", "TBANGLUONGCHITIET", "TBANGLUONGTONGHOP", "TLUUVET"
    ];

    /// <summary>Nhóm danh mục — sync 2 phút.</summary>
    public static readonly string[] Dictionary =
    [
        "DMATHANG", "DKHACHHANG", "DNHACUNGCAP", "DNHANVIEN", "DKHOHANG",
        "DCUAHANG", "DDINHLUONG", "DDOTKHUYENMAI", "DDOTKHUYENMAICHITIET",
        "DNHOMMATHANG", "DNHOMKHACHHANG", "DDONVITINH", "DLYDOTHUCHI", "DCUAHANG"
    ];

    public const int TransactionIntervalSeconds = 30;
    public const int DictionaryIntervalSeconds = 120;
    public const int SmallTablesIntervalSeconds = 600;
    public const int LedgerIntervalSeconds = 300;
    public static readonly TimeOnly ReconcileAt = new(2, 0);
    public static readonly TimeOnly MediaExtractAt = new(2, 30);
}
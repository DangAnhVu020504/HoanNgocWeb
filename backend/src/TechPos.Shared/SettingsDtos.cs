namespace TechPos.Shared;

/// <summary>1 cài đặt hệ thống trong W_APP_SETTING.</summary>
public sealed class AppSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Lưu nhiều cài đặt cùng lúc (key/value).</summary>
public sealed record AppSettingsSaveRequest(Dictionary<string, string?> Settings);

/// <summary>Thông tin công ty dùng cho header/footer PDF và hiển thị.</summary>
public sealed class CompanyInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? InvoiceFooter { get; set; }
}

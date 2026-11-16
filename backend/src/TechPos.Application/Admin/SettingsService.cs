using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Admin;

/// <summary>
/// Cài đặt hệ thống (W_APP_SETTING): thông tin công ty cho PDF, tham số điểm thân thiết,
/// nhắc chăm sóc khách hàng. Chỉ ghi các key trong whitelist.
/// </summary>
public sealed class SettingsService(SettingsRepository repo)
{
    public static readonly string[] AllowedKeys =
    [
        "COMPANY_NAME", "COMPANY_TAX_CODE", "COMPANY_PHONE", "COMPANY_EMAIL",
        "COMPANY_ADDRESS", "COMPANY_WEBSITE", "COMPANY_BANK_ACCOUNT", "COMPANY_BANK_NAME",
        "COMPANY_INVOICE_FOOTER", "LOYALTY_VND_PER_POINT", "LOYALTY_POINT_VALUE",
        "CRM_CARE_REMIND_DAYS"
    ];

    public Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default)
        => repo.GetAllAsync(ct);

    public async Task<CompanyInfoDto> GetCompanyInfoAsync(CancellationToken ct = default)
    {
        var s = await repo.GetDictionaryAsync(ct);
        return new CompanyInfoDto
        {
            Name = s.GetValueOrDefault("COMPANY_NAME") ?? "Cửa hàng Hoàn Ngọc",
            TaxCode = s.GetValueOrDefault("COMPANY_TAX_CODE"),
            Phone = s.GetValueOrDefault("COMPANY_PHONE"),
            Email = s.GetValueOrDefault("COMPANY_EMAIL"),
            Address = s.GetValueOrDefault("COMPANY_ADDRESS"),
            Website = s.GetValueOrDefault("COMPANY_WEBSITE"),
            BankAccount = s.GetValueOrDefault("COMPANY_BANK_ACCOUNT"),
            BankName = s.GetValueOrDefault("COMPANY_BANK_NAME"),
            InvoiceFooter = s.GetValueOrDefault("COMPANY_INVOICE_FOOTER")
        };
    }

    public async Task<string> GetCompanyNameAsync(CancellationToken ct = default)
        => await repo.GetValueAsync("COMPANY_NAME", ct) ?? "Cửa hàng Hoàn Ngọc";

    public async Task SaveAsync(AppSettingsSaveRequest req, CancellationToken ct = default)
    {
        var filtered = req.Settings
            .Where(kv => AllowedKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value);
        if (filtered.Count == 0)
            throw new ValidationException("Không có cài đặt hợp lệ để lưu", []);
        await repo.SaveAsync(filtered, ct);
    }
}

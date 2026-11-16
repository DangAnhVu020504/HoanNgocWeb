using System.Text.Json.Serialization;

namespace TechPos.Shared;

// ===================== Tích hợp COD / MoMo (Phase 8) =====================

/// <summary>Trạng thái cấu hình tích hợp — KHÔNG chứa secret, an toàn để trả cho admin.</summary>
public sealed record IntegrationStatusDto(
    bool Enabled,
    bool Configured,
    string Provider,
    IReadOnlyList<string> Missing);

public sealed record IntegrationsStatusDto(
    IntegrationStatusDto Cod,
    [property: JsonPropertyName("momo")] IntegrationStatusDto MoMo);

public sealed record MoMoCreatePaymentRequest(
    string DonHangId,
    decimal? Amount = null,
    string? OrderInfo = null);

public sealed class PaymentOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public decimal TongCong { get; set; }
    public decimal ConNo { get; set; }
    public string? KhachHang { get; set; }
}

public sealed class MoMoCreatePaymentResult
{
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string? PayUrl { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? Deeplink { get; set; }
    public int ResultCode { get; set; }
    public string? Message { get; set; }
}

public sealed class MoMoPaymentStatusDto
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? ResultCode { get; set; }
    public string? Message { get; set; }
    public long? TransId { get; set; }
    public string? PayUrl { get; set; }
}

/// <summary>Body IPN MoMo gửi về (POST JSON, không cần auth — xác thực bằng chữ ký).</summary>
public sealed class MoMoIpnRequest
{
    public string PartnerCode { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string? OrderInfo { get; set; }
    public string? OrderType { get; set; }
    public long TransId { get; set; }
    public int ResultCode { get; set; }
    public string? Message { get; set; }
    public string? PayType { get; set; }
    public long ResponseTime { get; set; }
    public string? ExtraData { get; set; }
    public string Signature { get; set; } = string.Empty;
}

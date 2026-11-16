using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TechPos.Data.MySql;
using TechPos.Shared;
using ValidationException = TechPos.Shared.ValidationException;

namespace TechPos.Application.Payments;

/// <summary>
/// MoMo Payment Gateway v2 (captureWallet). Cấu hình qua env Integrations__MoMo__*.
/// Luồng: create → trả payUrl/qrCodeUrl → IPN xác thực HMAC-SHA256 → cập nhật W_PAYMENT_TXN.
/// Không tự trừ công nợ TDONHANG — kế toán xác nhận phiếu thu để tránh ghi trùng.
/// </summary>
public sealed class MoMoService(PaymentRepository repo, IOptions<IntegrationOptions> options,
    IHttpClientFactory httpFactory)
{
    public const string Provider = "MOMO";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private sealed class MoMoApiResponse
    {
        public string? PayUrl { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? Deeplink { get; set; }
        public int ResultCode { get; set; }
        public string? Message { get; set; }
        public string? OrderId { get; set; }
        public string? RequestId { get; set; }
    }

    public async Task<MoMoCreatePaymentResult> CreatePaymentAsync(
        MoMoCreatePaymentRequest req, CancellationToken ct = default)
    {
        var cfg = options.Value.MoMo;
        if (!cfg.IsConfigured)
            throw new ValidationException("Cổng MoMo chưa được cấu hình (Integrations:MoMo)", []);
        if (string.IsNullOrWhiteSpace(req.DonHangId))
            throw new ValidationException("Chưa chọn đơn hàng", []);

        var order = await repo.GetOrderPaymentInfoAsync(req.DonHangId, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn hàng {req.DonHangId}");

        var outstanding = order.ConNo;
        var amount = req.Amount ?? outstanding;
        if (amount <= 0)
            throw new ValidationException("Đơn hàng không còn số tiền cần thanh toán", []);
        if (amount > outstanding)
            throw new ValidationException($"Số tiền vượt quá công nợ còn lại ({outstanding:N0} đ)", []);

        var amountLong = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        var orderId = "MM" + Guid.NewGuid().ToString("N");
        var requestId = Guid.NewGuid().ToString("N");
        var orderInfo = string.IsNullOrWhiteSpace(req.OrderInfo)
            ? $"Thanh toan don hang {order.SoPhieu}"
            : req.OrderInfo.Trim();
        const string extraData = "";

        var signature = BuildCreateSignature(cfg, amountLong, orderId, requestId, orderInfo, extraData);
        var body = new Dictionary<string, object?>
        {
            ["partnerCode"] = cfg.PartnerCode,
            ["requestId"] = requestId,
            ["amount"] = amountLong,
            ["orderId"] = orderId,
            ["orderInfo"] = orderInfo,
            ["redirectUrl"] = cfg.RedirectUrl,
            ["ipnUrl"] = cfg.IpnUrl,
            ["lang"] = "vi",
            ["extraData"] = extraData,
            ["requestType"] = cfg.RequestType,
            ["signature"] = signature,
        };

        await repo.InsertAsync(new PaymentTxn
        {
            Id = Guid.NewGuid().ToString(),
            Provider = Provider,
            OrderId = orderId,
            RequestId = requestId,
            DonHangId = order.DonHangId,
            Amount = amountLong,
            OrderInfo = orderInfo,
            Status = "PENDING",
        }, ct);

        try
        {
            var client = httpFactory.CreateClient("momo");
            using var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts),
                Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(cfg.Endpoint, content, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<MoMoApiResponse>(raw, JsonOpts) ?? new MoMoApiResponse();

            await repo.UpdateCreateResultAsync(orderId,
                parsed.ResultCode == 0 ? "PENDING" : "FAILED", parsed.ResultCode, parsed.Message,
                parsed.PayUrl, parsed.QrCodeUrl, parsed.Deeplink, raw, ct);

            if (!resp.IsSuccessStatusCode || parsed.ResultCode != 0)
                throw new ConflictException($"MoMo: {parsed.Message ?? "tạo giao dịch thất bại"}");

            return new MoMoCreatePaymentResult
            {
                OrderId = orderId,
                RequestId = requestId,
                PayUrl = parsed.PayUrl,
                QrCodeUrl = parsed.QrCodeUrl,
                Deeplink = parsed.Deeplink,
                ResultCode = parsed.ResultCode,
                Message = parsed.Message,
            };
        }
        catch (ValidationException) { throw; }
        catch (ConflictException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            await repo.UpdateCreateResultAsync(orderId, "FAILED", null,
                ex.Message, null, null, null, null, ct);
            throw new ValidationException("Không kết nối được MoMo — kiểm tra mạng/Endpoint", []);
        }
    }

    public async Task HandleIpnAsync(MoMoIpnRequest ipn, CancellationToken ct = default)
    {
        var cfg = options.Value.MoMo;
        if (!cfg.IsConfigured)
            throw new ValidationException("Cổng MoMo chưa được cấu hình (Integrations:MoMo)", []);

        var expected = BuildIpnSignature(cfg, ipn);
        if (!FixedTimeEquals(expected, ipn.Signature))
            throw new ValidationException("Chữ ký IPN MoMo không hợp lệ", []);

        var txn = await repo.GetByOrderIdAsync(Provider, ipn.OrderId, ct)
            ?? throw new NotFoundException($"Không tìm thấy giao dịch MoMo {ipn.OrderId}");
        if ((long)txn.Amount != ipn.Amount)
            throw new ValidationException("Số tiền IPN không khớp giao dịch", []);
        if (txn.Status == "PAID") return;

        var raw = JsonSerializer.Serialize(ipn, JsonOpts);
        await repo.UpdateIpnResultAsync(ipn.OrderId,
            ipn.ResultCode == 0 ? "PAID" : "FAILED", ipn.ResultCode, ipn.Message,
            ipn.TransId, raw, ct);
    }

    public async Task<MoMoPaymentStatusDto> GetStatusAsync(string orderId, CancellationToken ct = default)
    {
        var txn = await repo.GetByOrderIdAsync(Provider, orderId, ct)
            ?? throw new NotFoundException($"Không tìm thấy giao dịch MoMo {orderId}");
        return new MoMoPaymentStatusDto
        {
            OrderId = txn.OrderId,
            Status = txn.Status,
            Amount = txn.Amount,
            ResultCode = txn.ResultCode,
            Message = txn.Message,
            TransId = txn.TransId,
            PayUrl = txn.PayUrl,
        };
    }

    // ---------- Chữ ký (public static để unit test) ----------

    public static string BuildCreateSignature(MoMoOptions cfg, long amount, string orderId,
        string requestId, string orderInfo, string extraData)
    {
        var raw = $"accessKey={cfg.AccessKey}&amount={amount.ToString(CultureInfo.InvariantCulture)}" +
                  $"&extraData={extraData}&ipnUrl={cfg.IpnUrl}&orderId={orderId}&orderInfo={orderInfo}" +
                  $"&partnerCode={cfg.PartnerCode}&redirectUrl={cfg.RedirectUrl}&requestId={requestId}" +
                  $"&requestType={cfg.RequestType}";
        return HmacSha256(cfg.SecretKey, raw);
    }

    public static string BuildIpnSignature(MoMoOptions cfg, MoMoIpnRequest ipn)
    {
        var raw = $"accessKey={cfg.AccessKey}&amount={ipn.Amount.ToString(CultureInfo.InvariantCulture)}" +
                  $"&extraData={ipn.ExtraData}&message={ipn.Message}&orderId={ipn.OrderId}" +
                  $"&orderInfo={ipn.OrderInfo}&orderType={ipn.OrderType}&partnerCode={ipn.PartnerCode}" +
                  $"&payType={ipn.PayType}&requestId={ipn.RequestId}" +
                  $"&responseTime={ipn.ResponseTime.ToString(CultureInfo.InvariantCulture)}" +
                  $"&resultCode={ipn.ResultCode.ToString(CultureInfo.InvariantCulture)}" +
                  $"&transId={ipn.TransId.ToString(CultureInfo.InvariantCulture)}";
        return HmacSha256(cfg.SecretKey, raw);
    }

    public static string HmacSha256(string key, string data)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data)))
            .ToLowerInvariant();

    private static bool FixedTimeEquals(string expected, string actual)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
}

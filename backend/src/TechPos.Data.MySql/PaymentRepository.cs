using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>Thông tin đơn hàng tối thiểu để tạo giao dịch thanh toán.</summary>
public sealed class OrderPaymentInfo
{
    public string DonHangId { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public decimal TongCong { get; set; }
    public decimal ThanhToan { get; set; }
    public decimal ConNo { get; set; }
}

/// <summary>Giao dịch thanh toán online (W_PAYMENT_TXN) — dùng đối soát IPN.</summary>
public sealed class PaymentTxn
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string? DonHangId { get; set; }
    public decimal Amount { get; set; }
    public string? OrderInfo { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ResultCode { get; set; }
    public string? Message { get; set; }
    public long? TransId { get; set; }
    public string? PayUrl { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? Deeplink { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PaymentRepository(MySqlConnectionFactory factory)
{
    public async Task<OrderPaymentInfo?> GetOrderPaymentInfoAsync(string donHangId, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<OrderPaymentInfo>(
            """
            SELECT ID AS DonHangId, NAME AS SoPhieu,
                   COALESCE(TONGCONG,0) AS TongCong, COALESCE(THANHTOAN,0) AS ThanhToan,
                   COALESCE(CONNO,0) AS ConNo
            FROM TDONHANG
            WHERE ID = @id AND W_DELETED = 0
            """, new { id = donHangId });
    }

    /// <summary>Đơn web còn công nợ — gợi ý cho màn hình tạo giao dịch MoMo.</summary>
    public async Task<IReadOnlyList<PaymentOrderDto>> GetPayableOrdersAsync(CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        return (await conn.QueryAsync<PaymentOrderDto>(
            """
            SELECT ID AS Id, NAME AS SoPhieu, NGAY AS Ngay,
                   COALESCE(TONGCONG,0) AS TongCong, CONNO AS ConNo,
                   (SELECT NAME FROM DKHACHHANG k WHERE k.ID = h.DKHACHHANGID) AS KhachHang
            FROM TDONHANG h
            WHERE W_ORIGIN = 'WEB' AND W_DELETED = 0 AND STATUS = 30 AND CONNO > 0
            ORDER BY TIMECREATED DESC
            LIMIT 200
            """)).ToList();
    }

    public async Task InsertAsync(PaymentTxn txn, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO W_PAYMENT_TXN
            (ID, PROVIDER, ORDER_ID, REQUEST_ID, DONHANGID, AMOUNT, ORDER_INFO, STATUS,
             RESULT_CODE, MESSAGE, TRANS_ID, PAY_URL, QR_CODE_URL, DEEPLINK,
             RAW_CREATE_JSON, CREATED_AT, UPDATED_AT)
            VALUES
            (@Id, @Provider, @OrderId, @RequestId, @DonHangId, @Amount, @OrderInfo, @Status,
             @ResultCode, @Message, @TransId, @PayUrl, @QrCodeUrl, @Deeplink,
             NULL, NOW(3), NOW(3))
            """, txn);
    }

    public async Task UpdateCreateResultAsync(string orderId, string status, int? resultCode,
        string? message, string? payUrl, string? qrCodeUrl, string? deeplink,
        string? rawJson, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE W_PAYMENT_TXN
            SET STATUS = @status, RESULT_CODE = @resultCode, MESSAGE = @message,
                PAY_URL = @payUrl, QR_CODE_URL = @qrCodeUrl, DEEPLINK = @deeplink,
                RAW_CREATE_JSON = @rawJson, UPDATED_AT = NOW(3)
            WHERE PROVIDER = 'MOMO' AND ORDER_ID = @orderId
            """, new { orderId, status, resultCode, message, payUrl, qrCodeUrl, deeplink, rawJson });
    }

    public async Task<PaymentTxn?> GetByOrderIdAsync(string provider, string orderId, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<PaymentTxn>(
            """
            SELECT ID, PROVIDER, ORDER_ID AS OrderId, REQUEST_ID AS RequestId, DONHANGID AS DonHangId,
                   AMOUNT, ORDER_INFO AS OrderInfo, STATUS, RESULT_CODE AS ResultCode, MESSAGE,
                   TRANS_ID AS TransId, PAY_URL AS PayUrl, QR_CODE_URL AS QrCodeUrl, DEEPLINK,
                   CREATED_AT AS CreatedAt, UPDATED_AT AS UpdatedAt
            FROM W_PAYMENT_TXN
            WHERE PROVIDER = @provider AND ORDER_ID = @orderId
            """, new { provider, orderId });
    }

    public async Task UpdateIpnResultAsync(string orderId, string status, int resultCode,
        string? message, long? transId, string? rawJson, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE W_PAYMENT_TXN
            SET STATUS = @status, RESULT_CODE = @resultCode, MESSAGE = @message,
                TRANS_ID = @transId, RAW_IPN_JSON = @rawJson, UPDATED_AT = NOW(3)
            WHERE PROVIDER = 'MOMO' AND ORDER_ID = @orderId
            """, new { orderId, status, resultCode, message, transId, rawJson });
    }
}

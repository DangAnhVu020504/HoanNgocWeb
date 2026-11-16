namespace TechPos.Shared;

// =====================================================================
// Phase 8 — CSKH: chăm sóc khách hàng, phản hồi, tin nhắn, điểm thân thiết.
// Dữ liệu web-only lưu W_* (không ghi Firebird); khách hàng đọc mirror
// DKHACHHANG. Điểm thân thiết đồng bộ cột DIEMTICHLUYBANDAU của mirror.
// =====================================================================

// ---------- Chăm sóc khách hàng ----------

public sealed class CustomerCareDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? TenKhach { get; set; }
    public string? DienThoai { get; set; }
    public string? Email { get; set; }
    public string CareType { get; set; } = "CALL";
    public string? Content { get; set; }
    public string? Result { get; set; }
    public DateTime? NextCareAt { get; set; }
    public string? AssigneeUserId { get; set; }
    public string? AssigneeName { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed record CustomerCareQuery(
    string? Search = null,
    string? CareType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20);

public sealed record CustomerCareCreateRequest(
    string CustomerId,
    string CareType,
    string? Content,
    string? Result,
    DateTime? NextCareAt,
    string? AssigneeUserId);

public sealed record CustomerCareUpdateRequest(
    string? CareType,
    string? Content,
    string? Result,
    DateTime? NextCareAt,
    string? AssigneeUserId);

// ---------- Phản hồi khách hàng ----------

public sealed class CustomerFeedbackDto
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? TenKhach { get; set; }
    public string? InvoiceId { get; set; }
    public string? SoHoaDon { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
    public string Status { get; set; } = "NEW";
    public string? Response { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public sealed record CustomerFeedbackQuery(
    string? Search = null,
    string? Status = null,
    int? Rating = null,
    int Page = 1,
    int PageSize = 20);

public sealed record CustomerFeedbackCreateRequest(
    string? CustomerId,
    string? InvoiceId,
    int Rating,
    string? Content);

public sealed record CustomerFeedbackRespondRequest(string Response, string? Status);

// ---------- Tin nhắn (Email / SMS / Zalo) ----------

public sealed class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Channel { get; set; } = "EMAIL";
    public string? CustomerId { get; set; }
    public string? TenKhach { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? Error { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

public sealed record MessageQuery(
    string? Channel = null,
    string? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record MessageSendRequest(
    string Channel,
    string? CustomerId,
    string? Recipient,
    string? Subject,
    string Body);

// ---------- Điểm thân thiết ----------

public sealed class LoyaltyTxnDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string TxnType { get; set; } = "ADJUST";
    public int Points { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class LoyaltySummaryDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string? TenKhach { get; set; }
    public string? DienThoai { get; set; }
    public int Points { get; set; }
    public int TotalEarned { get; set; }
    public int TotalRedeemed { get; set; }
    public DateTime? LastTxnAt { get; set; }
}

public sealed record LoyaltyQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record LoyaltyAdjustRequest(string CustomerId, int Points, string? Note);

public sealed record LoyaltyAdjustResult(int Applied, int NewPoints);

namespace TechPos.Shared;

// =====================================================================
// Phase 7 — Email + Notification (docs/email/EMAIL-ARCHITECTURE.md):
//   W_EMAIL_TEMPLATE / W_EMAIL_EVENT / W_EMAIL_QUEUE / W_EMAIL_RECIPIENT /
//   W_EMAIL_LOG / W_EMAIL_SETTINGS + W_NOTIFICATION(+READ)
// Enqueue fire-and-forget: lỗi email KHÔNG BAO GIỜ fail transaction nghiệp vụ
// (mục 23) — gửi chỉ xảy ra trong EmailWorker sau khi transaction đã commit.
// =====================================================================

/// <summary>Mẫu email (W_EMAIL_TEMPLATE).</summary>
public sealed class EmailTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Request sửa template (Admin UI).</summary>
public sealed class EmailTemplateUpdateRequest
{
    public string Subject { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Event email — bật/tắt theo loại sự kiện (19 event seed sẵn).</summary>
public sealed class EmailEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public bool Enabled { get; set; }
    public string? RecipientRule { get; set; }
}

/// <summary>Dòng hàng đợi email (W_EMAIL_QUEUE + số người nhận).</summary>
public sealed class EmailQueueItemDto
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;   // PENDING|PROCESSING|SENT|FAILED|CANCELLED
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int SoNguoiNhan { get; set; }
}

/// <summary>Yêu cầu gửi email thủ công (Admin — Test Email / gửi trực tiếp).</summary>
public sealed record EmailSendRequest(
    string To,
    string? Name,
    string Subject,
    string? BodyHtml);

/// <summary>Cấu hình SMTP 1 key trong W_EMAIL_SETTINGS (password được xử lý riêng).</summary>
public sealed class EmailSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed record EmailSettingsSaveRequest(
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string FromEmail,
    string? FromName);

/// <summary>Thông báo (W_NOTIFICATION).</summary>
public sealed class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;    // SALES|INVENTORY|DEBT|COD|SYSTEM|EMAIL
    public string? Icon { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public sealed record NotificationListQuery(int Page = 1, int PageSize = 20, string? Group = null);

/// <summary>Trạng thái gửi từng người nhận (W_EMAIL_RECIPIENT).</summary>
public sealed class EmailRecipientDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "TO";
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? Error { get; set; }
}

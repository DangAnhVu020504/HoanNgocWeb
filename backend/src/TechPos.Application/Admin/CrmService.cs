using TechPos.Application.Notifications;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Admin;

/// <summary>
/// Use-case CSKH (Phase 8): chăm sóc khách hàng, phản hồi, tin nhắn đa kênh,
/// điểm thân thiết. Email dùng queue fire-and-forget; SMS/Zalo báo chưa cấu hình.
/// </summary>
public sealed class CrmService(CrmRepository repo, EmailService emails)
{
    private static readonly HashSet<string> CareTypes =
        new(StringComparer.OrdinalIgnoreCase) { "CALL", "EMAIL", "SMS", "ZALO", "VISIT", "OTHER" };
    private static readonly HashSet<string> Channels =
        new(StringComparer.OrdinalIgnoreCase) { "EMAIL", "SMS", "ZALO" };

    // ---------- Chăm sóc ----------

    public async Task<PageResult<CustomerCareDto>> SearchCareAsync(CustomerCareQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchCareAsync(q, ct);
        return new PageResult<CustomerCareDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<CustomerCareDto> GetCareAsync(string id, CancellationToken ct = default)
        => await repo.GetCareAsync(id, ct) ?? throw new NotFoundException($"Không tìm thấy phiếu chăm sóc {id}");

    public async Task<CustomerCareDto> CreateCareAsync(CustomerCareCreateRequest req, string userId, CancellationToken ct = default)
    {
        ValidateCareType(req.CareType);
        if (string.IsNullOrWhiteSpace(req.CustomerId))
            throw new ValidationException("Chưa chọn khách hàng",
                [new ApiError(nameof(req.CustomerId), "CUSTOMER_REQUIRED", "Chưa chọn khách hàng")]);
        var customer = await repo.GetCustomerContactAsync(req.CustomerId, ct)
            ?? throw new NotFoundException($"Không tìm thấy khách hàng {req.CustomerId}");

        var id = await repo.InsertCareAsync(req with { CareType = req.CareType.ToUpperInvariant() }, userId, ct);

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            _ = emails.EnqueueFromEventAsync("CUSTOMER_CARED",
                [(customer.Email!, customer.TenKhach)],
                new Dictionary<string, string?>
                {
                    ["TEN_KHACH"] = customer.TenKhach,
                    ["HINH_THUC"] = req.CareType.ToUpperInvariant(),
                    ["NOI_DUNG"] = req.Content,
                    ["KET_QUA"] = req.Result,
                    ["NGAY"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                }, "W_CUSTOMER_CARE", id, ct);
        }
        return await GetCareAsync(id, ct);
    }

    public async Task<CustomerCareDto> UpdateCareAsync(string id, CustomerCareUpdateRequest req, string userId, CancellationToken ct = default)
    {
        _ = await GetCareAsync(id, ct);
        if (!string.IsNullOrWhiteSpace(req.CareType)) ValidateCareType(req.CareType);
        var rows = await repo.UpdateCareAsync(id, req, userId, ct);
        if (rows == 0) throw new NotFoundException($"Không tìm thấy phiếu chăm sóc {id}");
        return await GetCareAsync(id, ct);
    }

    public async Task DeleteCareAsync(string id, CancellationToken ct = default)
    {
        _ = await GetCareAsync(id, ct);
        await repo.DeleteCareAsync(id, ct);
    }

    // ---------- Phản hồi ----------

    public async Task<PageResult<CustomerFeedbackDto>> SearchFeedbackAsync(CustomerFeedbackQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchFeedbackAsync(q, ct);
        return new PageResult<CustomerFeedbackDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<CustomerFeedbackDto> GetFeedbackAsync(string id, CancellationToken ct = default)
        => await repo.GetFeedbackAsync(id, ct) ?? throw new NotFoundException($"Không tìm thấy phản hồi {id}");

    public async Task<CustomerFeedbackDto> CreateFeedbackAsync(CustomerFeedbackCreateRequest req, string userId, CancellationToken ct = default)
    {
        if (req.Rating is < 1 or > 5)
            throw new ValidationException("Đánh giá phải từ 1 đến 5 sao",
                [new ApiError(nameof(req.Rating), "RATING_INVALID", "Đánh giá phải từ 1 đến 5")]);
        if (string.IsNullOrWhiteSpace(req.CustomerId) && string.IsNullOrWhiteSpace(req.InvoiceId))
            throw new ValidationException("Cần chọn khách hàng hoặc hóa đơn", []);
        if (!string.IsNullOrWhiteSpace(req.CustomerId))
            _ = await repo.GetCustomerContactAsync(req.CustomerId, ct)
                ?? throw new NotFoundException($"Không tìm thấy khách hàng {req.CustomerId}");

        var id = await repo.InsertFeedbackAsync(req, userId, ct);
        return await GetFeedbackAsync(id, ct);
    }

    public async Task<CustomerFeedbackDto> RespondFeedbackAsync(string id, CustomerFeedbackRespondRequest req, string userId, CancellationToken ct = default)
    {
        _ = await GetFeedbackAsync(id, ct);
        if (string.IsNullOrWhiteSpace(req.Response))
            throw new ValidationException("Nội dung phản hồi bắt buộc",
                [new ApiError(nameof(req.Response), "RESPONSE_REQUIRED", "Nội dung phản hồi bắt buộc")]);
        var status = string.IsNullOrWhiteSpace(req.Status) ? "RESOLVED" : req.Status.Trim().ToUpperInvariant();
        if (status is not ("NEW" or "PROCESSING" or "RESOLVED"))
            throw new ValidationException("Trạng thái phản hồi không hợp lệ", []);
        await repo.RespondFeedbackAsync(id, req with { Status = status }, userId, ct);
        return await GetFeedbackAsync(id, ct);
    }

    // ---------- Tin nhắn ----------

    public async Task<PageResult<MessageDto>> SearchMessagesAsync(MessageQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchMessagesAsync(q, ct);
        return new PageResult<MessageDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<MessageDto> SendMessageAsync(MessageSendRequest req, string userId, CancellationToken ct = default)
    {
        var channel = req.Channel.Trim().ToUpperInvariant();
        if (!Channels.Contains(channel))
            throw new ValidationException("Kênh gửi chỉ nhận EMAIL, SMS hoặc ZALO",
                [new ApiError(nameof(req.Channel), "CHANNEL_INVALID", "Kênh gửi không hợp lệ")]);
        if (string.IsNullOrWhiteSpace(req.Body))
            throw new ValidationException("Nội dung tin nhắn bắt buộc",
                [new ApiError(nameof(req.Body), "BODY_REQUIRED", "Nội dung tin nhắn bắt buộc")]);

        CrmRepository.CustomerContactRow? customer = null;
        if (!string.IsNullOrWhiteSpace(req.CustomerId))
            customer = await repo.GetCustomerContactAsync(req.CustomerId, ct)
                ?? throw new NotFoundException($"Không tìm thấy khách hàng {req.CustomerId}");

        var recipient = req.Recipient?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            recipient = channel == "EMAIL" ? customer?.Email : customer?.DienThoai;
        }
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ValidationException(channel == "EMAIL"
                ? "Khách hàng chưa có email và chưa nhập người nhận"
                : "Khách hàng chưa có số điện thoại và chưa nhập người nhận",
                [new ApiError(nameof(req.Recipient), "RECIPIENT_REQUIRED", "Chưa có người nhận")]);

        var subject = req.Subject?.Trim();
        if (channel == "EMAIL" && string.IsNullOrWhiteSpace(subject))
            subject = "Thông báo từ Hoàn Ngọc";

        string id;
        if (channel == "EMAIL")
        {
            id = await repo.InsertMessageAsync(channel, req.CustomerId, recipient, subject, req.Body,
                "PENDING", null, userId, ct);
            var queueId = await emails.EnqueueAsync("CUSTOMER_MESSAGE", subject ?? "Thông báo", req.Body,
                [(recipient, customer?.TenKhach)], "W_MESSAGE", id, ct);
            if (queueId is null)
                await repo.UpdateMessageStatusAsync(id, "FAILED", "Không đưa được email vào hàng đợi", ct);
        }
        else
        {
            id = await repo.InsertMessageAsync(channel, req.CustomerId, recipient, subject, req.Body,
                "NOT_CONFIGURED", "Chưa cấu hình nhà cung cấp SMS/Zalo", userId, ct);
        }
        return await repo.GetMessageAsync(id, ct)
               ?? throw new NotFoundException($"Không tìm thấy tin nhắn {id}");
    }

    // ---------- Điểm thân thiết ----------

    public async Task<PageResult<LoyaltySummaryDto>> SearchLoyaltyAsync(LoyaltyQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchLoyaltyAsync(q, ct);
        return new PageResult<LoyaltySummaryDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public Task<IReadOnlyList<LoyaltyTxnDto>> GetLoyaltyTxnsAsync(string customerId, CancellationToken ct = default)
        => repo.GetLoyaltyTxnsAsync(customerId, 200, ct);

    public async Task<LoyaltyAdjustResult> AdjustLoyaltyAsync(LoyaltyAdjustRequest req, string userId, CancellationToken ct = default)
    {
        if (req.Points == 0)
            throw new ValidationException("Số điểm điều chỉnh phải khác 0",
                [new ApiError(nameof(req.Points), "POINTS_ZERO", "Số điểm phải khác 0")]);
        _ = await repo.GetCustomerContactAsync(req.CustomerId, ct)
            ?? throw new NotFoundException($"Không tìm thấy khách hàng {req.CustomerId}");

        var (applied, newPoints) = await repo.AdjustLoyaltyAsync(req.CustomerId, req.Points, req.Note, userId, ct);
        return new LoyaltyAdjustResult(applied, newPoints);
    }

    private static void ValidateCareType(string careType)
    {
        if (string.IsNullOrWhiteSpace(careType) || !CareTypes.Contains(careType))
            throw new ValidationException("Hình thức chăm sóc không hợp lệ",
                [new ApiError(nameof(careType), "CARE_TYPE_INVALID", "Hình thức chăm sóc không hợp lệ")]);
    }
}

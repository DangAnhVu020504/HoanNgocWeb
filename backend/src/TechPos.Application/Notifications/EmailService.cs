using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Notifications;

/// <summary>
/// Gửi email qua SMTP (System.Net.Mail — không thêm package, ADR-07: không thêm dependency
/// khi BCL đủ). Lỗi SMTP ném exception — EmailWorker bắt và retry, không lan ra nghiệp vụ.
/// </summary>
public interface IEmailSender
{
    Task<(bool Ok, string? Response, string? Error)> SendAsync(
        string host, int port, bool useSsl, string? username, string? password,
        string fromEmail, string? fromName,
        string toEmail, string? toName, string subject, string? bodyHtml,
        CancellationToken ct = default);
}

public sealed class SmtpEmailSender : IEmailSender
{
    public async Task<(bool Ok, string? Response, string? Error)> SendAsync(
        string host, int port, bool useSsl, string? username, string? password,
        string fromEmail, string? fromName,
        string toEmail, string? toName, string subject, string? bodyHtml,
        CancellationToken ct = default)
    {
        try
        {
            using var client = new System.Net.Mail.SmtpClient(host, port)
            {
                EnableSsl = useSsl,
                Credentials = string.IsNullOrEmpty(username)
                    ? null
                    : new System.Net.NetworkCredential(username, password),
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                Timeout = 15_000
            };
            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromEmail, fromName ?? fromEmail),
                Subject = subject,
                Body = bodyHtml ?? string.Empty,
                IsBodyHtml = !string.IsNullOrEmpty(bodyHtml),
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            message.To.Add(new System.Net.Mail.MailAddress(toEmail, toName ?? toEmail));
            await client.SendMailAsync(message, ct);
            return (true, "OK", null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}

/// <summary>
/// Use-case Email Admin (Phase 7): templates, events, queue management, settings, test-send.
/// EnqueueFromEventAsync: fire-and-forget — kiểm tra event ENABLED + render template; lỗi SMTP
/// tuyệt đối không lan ra caller (bắt mọi exception).
/// </summary>
public sealed class EmailService(EmailRepository repo, IEmailSender sender)
{
    public Task<List<EmailTemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
        => repo.GetTemplatesAsync(ct);

    public Task UpdateTemplateAsync(string code, EmailTemplateUpdateRequest req, CancellationToken ct = default)
        => repo.UpdateTemplateAsync(code, req, ct);

    public Task<List<EmailEventDto>> GetEventsAsync(CancellationToken ct = default)
        => repo.GetEventsAsync(ct);

    public Task SetEventEnabledAsync(string code, bool enabled, CancellationToken ct = default)
        => repo.SetEventEnabledAsync(code, enabled, ct);

    public Task<(IReadOnlyList<EmailQueueItemDto> Items, long Total)> SearchQueueAsync(
        string? status, int page, int pageSize, CancellationToken ct = default)
        => repo.SearchQueueAsync(status, page, pageSize, ct);

    public Task<bool> RetryQueueItemAsync(string id, CancellationToken ct = default)
        => repo.RetryQueueItemAsync(id, ct);

    public Task<bool> CancelQueueItemAsync(string id, CancellationToken ct = default)
        => repo.CancelQueueItemAsync(id, ct);

    public async Task<Dictionary<string, string?>> GetSettingsAsync(CancellationToken ct = default)
    {
        var s = await repo.GetSettingsAsync(ct);
        if (s.ContainsKey("PASSWORD")) s["PASSWORD"] = s["PASSWORD"] is { Length: > 0 } ? "••••••" : "";
        return s;
    }

    public async Task SaveSettingsAsync(EmailSettingsSaveRequest req, CancellationToken ct = default)
    {
        await repo.SaveSettingAsync("HOST", req.Host, ct);
        await repo.SaveSettingAsync("PORT", req.Port.ToString(), ct);
        await repo.SaveSettingAsync("USE_SSL", req.UseSsl ? "1" : "0", ct);
        await repo.SaveSettingAsync("USERNAME", req.Username, ct);
        if (!string.IsNullOrEmpty(req.Password)) await repo.SaveSettingAsync("PASSWORD", req.Password, ct);
        await repo.SaveSettingAsync("FROM_EMAIL", req.FromEmail, ct);
        await repo.SaveSettingAsync("FROM_NAME", req.FromName, ct);
    }

    /// <summary>Test gửi 1 email trực tiếp (Admin — [Test Email] EMAIL-ARCHITECTURE §2).</summary>
    public async Task<(bool Ok, string? Error)> TestSendAsync(EmailSendRequest req, CancellationToken ct = default)
    {
        var s = await repo.GetSettingsAsync(ct);
        var (ok, _, err) = await sender.SendAsync(
            s.GetValueOrDefault("HOST") ?? "", int.TryParse(s.GetValueOrDefault("PORT"), out var p) ? p : 587,
            s.GetValueOrDefault("USE_SSL") != "0",
            s.GetValueOrDefault("USERNAME"), s.GetValueOrDefault("PASSWORD"),
            s.GetValueOrDefault("FROM_EMAIL") ?? "no-reply@localhost", s.GetValueOrDefault("FROM_NAME"),
            req.To, req.Name, req.Subject, req.BodyHtml, ct);
        return (ok, err);
    }

    /// <summary>
    /// Gửi mail thủ công vào queue (fire-and-forget). Trả về queue id hoặc null (không ném).
    /// </summary>
    public Task<string?> EnqueueAsync(string eventType, string subject, string? bodyHtml,
        IReadOnlyList<(string Email, string? Name)> recipients,
        string? referenceType = null, string? referenceId = null, CancellationToken ct = default)
        => repo.EnqueueAsync(eventType, subject, bodyHtml, recipients, referenceType, referenceId, ct);

    /// <summary>
    /// Enqueue theo EVENT đã cấu hình (render template + biến). Fire-and-forget: event tắt /
    /// template thiếu / DB lỗi ⇒ trả false, KHÔNG ném exception (mục 23 — email fail không fail transaction).
    /// </summary>
    public async Task<bool> EnqueueFromEventAsync(string eventType,
        IReadOnlyList<(string Email, string? Name)> recipients,
        Dictionary<string, string?> variables,
        string? referenceType = null, string? referenceId = null, CancellationToken ct = default)
    {
        try
        {
            var ev = await repo.GetEventAsync(eventType, ct);
            if (ev is null || !ev.Enabled) return false;

            string subject;
            string? bodyHtml;
            if (!string.IsNullOrWhiteSpace(ev.TemplateCode))
            {
                var tpl = await repo.GetTemplateByCodeAsync(ev.TemplateCode!, ct);
                if (tpl is null) return false;
                subject = Render(tpl.Subject, variables);
                bodyHtml = Render(tpl.BodyHtml, variables);
            }
            else
            {
                subject = Render(eventType, variables);
                bodyHtml = Render(string.Join("<br/>", variables.Select(kv => $"{kv.Key}: {kv.Value}")), variables);
            }

            await repo.EnqueueAsync(eventType, subject, bodyHtml, recipients, referenceType, referenceId, ct);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Thay {{VAR}} trong template — giá trị được escape HTML (chống XSS, §4).</summary>
    public static string Render(string? template, Dictionary<string, string?> variables)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        var result = template;
        foreach (var (key, value) in variables)
            result = result.Replace("{{" + key + "}}", System.Net.WebUtility.HtmlEncode(value ?? ""));
        return result;
    }
}

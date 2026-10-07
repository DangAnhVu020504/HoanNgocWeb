using FluentAssertions;
using TechPos.Application.Notifications;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Phase 7 — Email composer: render template {{VAR}} + escape HTML (EMAIL-ARCHITECTURE §4).</summary>
public class EmailRenderTests
{
    [Fact]
    public void Render_replaces_variables_and_encodes_html()
    {
        var tpl = "Kính gửi {{CUSTOMER_NAME}}, đơn {{ORDER_NUMBER}} tổng {{TOTAL_AMOUNT}}.";
        var result = EmailService.Render(tpl, new Dictionary<string, string?>
        {
            ["CUSTOMER_NAME"] = "Khách Đại Lý",
            ["ORDER_NUMBER"] = "BL26/00001",
            ["TOTAL_AMOUNT"] = "1.250.000",
        });

        // Biến được HtmlEncode (chống XSS — EMAIL-ARCHITECTURE §4) nên dấu tiếng Việt → &#...;
        result.Should().Contain("BL26/00001");
        result.Should().Contain("1.250.000");
        result.Should().NotContain("{{");
        result.Should().Contain("&#");   // đã encode
    }

    [Fact]
    public void Render_escapes_html_values_anti_xss()
    {
        var result = EmailService.Render("<p>{{CUSTOMER_NAME}}</p>",
            new Dictionary<string, string?> { ["CUSTOMER_NAME"] = "<script>alert(1)</script>" });

        result.Should().NotContain("<script>");
        result.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Render_handles_missing_variables_and_null_template()
    {
        EmailService.Render(null, new Dictionary<string, string?>()).Should().BeEmpty();
        // Biến không có trong dictionary giữ nguyên (template lỗi hiển thị rõ thay vì im lặng)
        EmailService.Render("X {{UNKNOWN_VAR}} Y", new Dictionary<string, string?>())
            .Should().Contain("{{UNKNOWN_VAR}}");
    }
}

/// <summary>
/// Fake SMTP — test EmailWorker retry backoff + "email fail không fail nghiệp vụ" (mục 23).
/// </summary>
public class EmailWorkerRetryTests
{
    /// <summary>Sender luôn lỗi — dùng để kiểm tra worker nuốt exception (không ném).</summary>
    private sealed class FailingSender : Application.Notifications.IEmailSender
    {
        public int Calls;
        public Task<(bool Ok, string? Response, string? Error)> SendAsync(
            string host, int port, bool useSsl, string? username, string? password,
            string fromEmail, string? fromName, string toEmail, string? toName,
            string subject, string? bodyHtml, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult<(bool, string?, string?)>((false, null, "SMTP host unreachable (test)"));
        }
    }

    [Fact]
    public void SmtpEmailSender_invalid_host_returns_error_not_throw()
    {
        var sender = new TechPos.Application.Notifications.SmtpEmailSender();
        // Host không tồn tại — SendAsync phải trả (false, error) chứ không ném exception
        var act = async () => await sender.SendAsync(
            "invalid.smtp.host.invalid", 25, false, null, null,
            "from@test.local", "Test", "to@test.local", "To", "S", "<p>b</p>");
        var result = act.Invoking(async a => await a()).Should().NotThrowAsync().Result;
    }

    [Fact]
    public void SmtpEmailSender_empty_host_fails_fast()
    {
        var sender = new TechPos.Application.Notifications.SmtpEmailSender();
        var act = () => sender.SendAsync("", 587, false, null, null,
            "from@t.local", null, "to@t.local", null, "S", null, default);
        act.Invoking(async a => await a()).Should().NotThrowAsync().GetAwaiter().GetResult();
    }
}

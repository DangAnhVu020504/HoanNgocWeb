using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TechPos.Api.Middleware;
using TechPos.Api.Hubs;
using TechPos.Api.Workers;
using TechPos.Application.Auth;
using TechPos.Application.Catalog;
using TechPos.Application.Cashbook;
using TechPos.Application.Hr;
using TechPos.Application.Inventory;
using TechPos.Application.Notifications;
using TechPos.Application.Pos;
using TechPos.Application.Products;
using TechPos.Application.Reports;
using TechPos.Application.Sales;
using TechPos.Data.MySql;
using TechPos.Shared;
using TechPos.Sync.Core;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();
builder.Services.AddSingleton(Serilog.Log.Logger);

// ----- Options (mục 53: secret qua env, không commit) -----
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Section));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.Configure<FeatureFlagOptions>(builder.Configuration.GetSection(FeatureFlagOptions.Section));
var dbOptions = builder.Configuration.GetSection(DatabaseOptions.Section).Get<DatabaseOptions>() ?? new();
var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new();

// ----- Data / Application DI -----
builder.Services.AddSingleton(_ => new MySqlConnectionFactory(dbOptions.MySqlConnectionString));
builder.Services.AddSingleton(_ => new JwtTokenService(jwtOptions));
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddScoped<AuthService>();

// ----- Phase 4: Danh mục mặt hàng (mục 60) -----
builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddScoped<ProductService>();

// ----- Phase 4: Danh mục khách hàng / NCC / kho hàng -----
builder.Services.AddSingleton<CustomerRepository>();
builder.Services.AddSingleton<SupplierRepository>();
builder.Services.AddSingleton<WarehouseRepository>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<WarehouseService>();

// ----- Phase 4: POS Bán lẻ (mục 4.1) -----
builder.Services.AddSingleton<PosSaleRepository>();
builder.Services.AddScoped<PosSaleService>();

// ----- Phase 4: Kho — Tồn kho/Thẻ kho (4.8) + Phiếu nhập (4.4) + Kiểm kê (4.7) -----
builder.Services.AddSingleton<InventoryRepository>();
builder.Services.AddSingleton<ReceiptRepository>();
builder.Services.AddSingleton<StocktakeRepository>();
builder.Services.AddScoped<InventoryService>();

// ----- Phase 4: Kho — Xuất kho (4.5) + Chuyển kho (4.6) + Xuất trả NCC -----
builder.Services.AddSingleton<StockIssueRepository>();
builder.Services.AddSingleton<StockTransferRepository>();
builder.Services.AddSingleton<SupplierReturnRepository>();

// ----- Phase 4.8: Tính lại giá vốn -----
builder.Services.AddSingleton<CostRecalcRepository>();
builder.Services.AddScoped<CostRecalcService>();

// ----- Phase 5.1–5.2: Quỹ (phiếu thu/chi, tồn quỹ) + Công nợ KH/NCC -----
builder.Services.AddSingleton<CashbookRepository>();
builder.Services.AddSingleton<DebtRepository>();
builder.Services.AddScoped<CashbookService>();
builder.Services.AddScoped<DebtService>();

// ----- Phase 5.3: Nhân sự (nhân viên, chấm công, thưởng phạt, tính lương, giấy giới thiệu) -----
builder.Services.AddSingleton<HrRepository>();
builder.Services.AddScoped<HrService>();

// ----- Phase 4: Bán sỉ (4.2) + Trả lại KH (4.3) -----
builder.Services.AddSingleton<WholesaleRepository>();
builder.Services.AddSingleton<CustomerReturnRepository>();
builder.Services.AddScoped<WholesaleService>();
builder.Services.AddScoped<CustomerReturnService>();

// ----- Phase 4.9–4.10: Báo giá / Đặt hàng / Đặt mua NCC / Hợp đồng / COD / Giao xe -----
builder.Services.AddSingleton<CustomerDocRepository>();
builder.Services.AddSingleton<PurchaseOrderRepository>();
builder.Services.AddSingleton<ContractRepository>();
builder.Services.AddSingleton<CodRepository>();
builder.Services.AddSingleton<VehicleDeliveryRepository>();
builder.Services.AddScoped<CustomerDocService>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddSingleton<LookupRepository>();

// ----- Phase 6: Report Center (báo cáo + Excel + PDF + barcode) -----
builder.Services.AddSingleton<ReportRepository>();
builder.Services.AddScoped<ReportService>();

// ----- Phase 7: Email + Notification (queue worker, retry backoff, SignalR) -----
builder.Services.AddSingleton<EmailRepository>();
builder.Services.AddSingleton<NotificationRepository>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<TechPos.Api.Workers.EmailWorker>();
builder.Services.AddHostedService<TechPos.Api.Workers.ScheduledJobsWorker>();
builder.Services.AddSignalR();

// ----- Phase 6.4: Nhập Excel danh mục (mặt hàng / khách hàng) -----
builder.Services.AddScoped<CatalogImportService>();

// ----- Sync metadata (SyncController đọc/ghi qua MySQL — không cần Firebird) -----
builder.Services.AddSingleton<SyncMetaRepository.MySqlConnectionFactoryLike>(sp =>
    new ApiMetaAdapter(sp.GetRequiredService<MySqlConnectionFactory>()));
builder.Services.AddSingleton<SyncMetaRepository>();

// ----- Lịch sử chứng từ (hợp nhất mọi bảng mirror W_ORIGIN='WEB') -----
builder.Services.AddSingleton<HistoryRepository>();

// ----- Controllers + Auth -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
        // SignalR WebSocket không gửi được Authorization header — đọc access_token từ query
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ----- Migration on startup (idempotent) -----
try
{
    var migrationsFolder = Path.Combine(app.Environment.ContentRootPath, "..", "..", "..", "database", "migrations");
    var runner = new MigrationRunner(new MySqlConnectionFactory(dbOptions.MySqlConnectionString), migrationsFolder, Log.Logger);
    await runner.RunAllAsync();
    Log.Information("Database migrations OK");
}
catch (Exception ex)
{
    Log.Error(ex, "Database migration failed — API tiếp tục chạy nhưng chức năng DB sẽ lỗi");
}

// ----- Pipeline -----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Phase 9: serve web SPA build (frontend/techpos-web/dist) + SPA fallback — chạy 1 cổng duy nhất
var webDist = Path.Combine(app.Environment.ContentRootPath, "..", "..", "..", "frontend", "techpos-web", "dist");
if (Directory.Exists(webDist))
{
    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webDist)
    });
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webDist)
    });
}

// Phase 7: realtime notification hub (JWT qua query string ?access_token=)
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// ---------- Adapter: Data.MySql → Sync.Core meta interface ----------
internal sealed class ApiMetaAdapter(MySqlConnectionFactory factory) : SyncMetaRepository.MySqlConnectionFactoryLike
{
    public async Task<MySqlConnector.MySqlConnection> OpenAsync(CancellationToken ct = default)
        => await factory.OpenAsync(ct);
}

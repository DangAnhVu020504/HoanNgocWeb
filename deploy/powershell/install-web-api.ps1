# ============================================================
# install-web-api.ps1 — Publish + cài TechPos Web API (Phase 9, không Docker)
# Usage (PowerShell Administrator):
#   .\install-web-api.ps1 [-InstallDir "C:\TechPosApi"] [-ServiceName "TechPosApi"]
# Sau khi cài: cấu hình appsettings.json (connection string + JWT secret) rồi Start-Service.
# Reverse proxy: đặt IIS/kestrel phía trước hoặc mở trực tiếp cổng (LAN nội bộ).
# ============================================================
param(
    [string]$InstallDir = "C:\TechPosApi",
    [string]$ServiceName = "TechPosApi",
    [int]$Port = 5005
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$proj = Join-Path $root "backend\src\TechPos.Api\TechPos.Api.csproj"

Write-Host "=== 1. Publish (win-x64) ===" -ForegroundColor Cyan
dotnet publish $proj -c Release -r win-x64 --self-contained false -o $InstallDir
if ($LASTEXITCODE -ne 0) { throw "Publish lỗi" }

Write-Host "=== 2. Cấu hình URL qua appsettings.Production.json (nếu chưa có) ===" -ForegroundColor Cyan
$prodSettings = Join-Path $InstallDir "appsettings.Production.json"
if (-not (Test-Path $prodSettings)) {
    @"
{
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:$Port" } } },
  "Database": { "MySqlConnectionString": "server=localhost;port=3306;database=techposweb;user id=hn_api;password=DOI_MAT_KHAU;default command timeout=60;guidformat=None;" },
  "Jwt": { "SecretKey": "DOI_SECRET_TOI_THIEU_32_KY_TU__SET_ENV_TRONG_PRODUCTION" }
}
"@ | Set-Content -LiteralPath $prodSettings -Encoding UTF8
    Write-Host "  Đã tạo $prodSettings — BẮT BUỘC sửa connection string + JWT secret trước khi chạy."
}

Write-Host "=== 3. Cài Windows Service ===" -ForegroundColor Cyan
$exe = Join-Path $InstallDir "TechPos.Api.exe"
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service $ServiceName -Force
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}
sc.exe create $ServiceName binPath= "`"$exe`"" start= auto DisplayName= "TechPos Web API (MySQL + JWT + SignalR)"
sc.exe description $ServiceName "Web API cho TechPosWeb — ASP.NET Core 10, đọc/ghi mirror MySQL. Migration tự chạy khi khởi động."
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

Write-Host ""
Write-Host "Sau khi sửa cấu hình: Start-Service $ServiceName" -ForegroundColor Green
Write-Host "Kiểm tra: GET http://localhost:$Port/api/health"
Write-Host "Tạo admin đầu tiên: dotnet run --project backend\tools\TechPos.AdminSeed -- `"<conn`" admin MatKhau@123"

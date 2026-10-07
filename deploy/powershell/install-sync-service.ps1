# ============================================================
# install-sync-service.ps1 — Publish + cài TechPos Sync Service (mục 54, không Docker)
# Usage (PowerShell Administrator):
#   .\install-sync-service.ps1 [-InstallDir "C:\TechPosSync"] [-ServiceName "TechPosSync"]
# Yêu cầu: .NET 10 runtime; chạy CÙNG MÁY với bản copy DEMO.FDB (xem PHASE0-FINDINGS).
# ============================================================
param(
    [string]$InstallDir = "C:\TechPosSync",
    [string]$ServiceName = "TechPosSync"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$proj = Join-Path $root "backend\src\TechPos.Sync.Service\TechPos.Sync.Service.csproj"

Write-Host "=== 1. Publish (win-x86 vì fbembed.dll là 32-bit) ===" -ForegroundColor Cyan
dotnet publish $proj -c Release -r win-x86 --self-contained false -o $InstallDir
if ($LASTEXITCODE -ne 0) { throw "Publish lỗi" }

Write-Host "=== 2. Copy fbembed.dll + ICU 3.0 (bắt buộc cho Embedded) ===" -ForegroundColor Cyan
$fbSource = "C:\Program Files (x86)\TechPos\SM4\v4.5"
foreach ($f in @("fbembed.dll", "icudt30.dll", "icuin30.dll", "icuuc30.dll")) {
    Copy-Item (Join-Path $fbSource $f) $InstallDir -Force
}

Write-Host "=== 3. Cài Windows Service ===" -ForegroundColor Cyan
$exe = Join-Path $InstallDir "TechPos.Sync.Service.exe"
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service $ServiceName -Force
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}
sc.exe create $ServiceName binPath= "`"$exe`"" start= auto DisplayName= "TechPos Sync Service (Firebird → MySQL)"
sc.exe description $ServiceName "Đồng bộ Firebird 2.5 Embedded → MySQL 8 cho TechPosWeb. READ-ONLY trên Firebird."
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

Write-Host "=== 4. Kiểm tra cấu hình ===" -ForegroundColor Yellow
Write-Host "  Sửa file: $InstallDir\appsettings.json"
Write-Host "  - Database:MySqlConnectionString  (bắt buộc)"
Write-Host "  - Database:FirebirdDatabasePath   (trỏ tới BẢN COPY của DEMO.FDB, không phải DB gốc trong Program Files)"
Write-Host "  - Database:FirebirdClientLibrary  ($InstallDir\fbembed.dll)"
Write-Host ""
Write-Host "Sau khi sửa: Start-Service $ServiceName" -ForegroundColor Green
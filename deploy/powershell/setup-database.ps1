# ============================================================
# setup-database.ps1 — Tạo schema MySQL techposweb + chạy migration + seed
# Usage:
#   .\setup-database.ps1 -MySqlRootPassword "xxx" [-Server localhost] [-Database techposweb]
# Không động tới Firebird (mục 51).
# ============================================================
param(
    [Parameter(Mandatory = $true)][string]$MySqlRootPassword,
    [string]$Server = "localhost",
    [int]$Port = 3306,
    [string]$Database = "techposweb",
    [string]$MySqlExe = "mysql"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$migrations = Join-Path $root "database\migrations"
$seed = Join-Path $root "database\seed"

Write-Host "=== 1. Tạo database $Database ===" -ForegroundColor Cyan
& $MySqlExe -h $Server -P $Port -u root "--password=$MySqlRootPassword" -e "CREATE DATABASE IF NOT EXISTS $Database CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;"
if ($LASTEXITCODE -ne 0) { throw "Không tạo được database — kiểm tra mật khẩu/MySQL service." }

# Đổi mật khẩu placeholder trong appsettings trước khi dùng — KHÔNG commit mật khẩu thật.
Write-Host "=== 2. Chạy migrations (002 là mirror DDL sinh từ discovery Phase 0) ===" -ForegroundColor Cyan
Get-ChildItem $migrations -Filter *.sql | Sort-Object Name | ForEach-Object {
    Write-Host "  -> $($_.Name)"
    Get-Content $_.FullName -Raw | & $MySqlExe -h $Server -P $Port -u root "--password=$MySqlRootPassword" $Database
    if ($LASTEXITCODE -ne 0) { throw "Migration lỗi: $($_.Name)" }
}

Write-Host "=== 3. Seed roles/permissions/flags/email-events ===" -ForegroundColor Cyan
Get-ChildItem $seed -Filter *.sql | Sort-Object Name | ForEach-Object {
    Write-Host "  -> $($_.Name)"
    Get-Content $_.FullName -Raw | & $MySqlExe -h $Server -P $Port -u root "--password=$MySqlRootPassword" $Database
    if ($LASTEXITCODE -ne 0) { throw "Seed lỗi: $($_.Name)" }
}

Write-Host "=== 4. Kiểm tra ===" -ForegroundColor Cyan
& $MySqlExe -h $Server -P $Port -u root "--password=$MySqlRootPassword" $Database -e "SELECT COUNT(*) AS mirror_tables FROM information_schema.TABLES WHERE TABLE_SCHEMA='$Database' AND TABLE_NAME NOT LIKE 'W\_%'; SELECT COUNT(*) AS w_tables FROM information_schema.TABLES WHERE TABLE_SCHEMA='$Database' AND TABLE_NAME LIKE 'W\_%';"

Write-Host ""
Write-Host "XONG. Bước tiếp theo:" -ForegroundColor Green
Write-Host "  1. Sửa 'Database:MySqlConnectionString' trong backend\src\TechPos.Api\appsettings.json"
Write-Host "  2. Tạo admin:  dotnet run --project backend\tools\TechPos.AdminSeed -- `<conn`> admin MatKhau@123"
Write-Host "  3. Chạy sync:  dotnet run --project backend\src\TechPos.Sync.Service"
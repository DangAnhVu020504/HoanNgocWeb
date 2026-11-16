# ============================================================
# backup-mysql.ps1 — Backup MySQL hằng đêm (Phase 9 — RUNBOOK §3.1)
# Usage (khuyến nghị — không lộ mật khẩu):
#   mysql_config_editor set --login-path=techpos-backup --host=localhost --user=root --password
#   .\backup-mysql.ps1 -LoginPath techpos-backup [-BackupDir "D:\Backup"] [-KeepDays 14]
# Usage (dự phòng):
#   .\backup-mysql.ps1 -RootPassword "xxx"
# Task Scheduler: dùng register-backup-task.ps1 (23:30 hằng ngày)
# ============================================================
param(
    [string]$RootPassword,
    [string]$LoginPath,
    [string]$BackupDir = "D:\Backup",
    [int]$KeepDays = 14,
    [string]$MySqlDump = "mysqldump",
    [string]$Database = "techposweb"
)

$ErrorActionPreference = "Stop"
if (-not $LoginPath -and -not $RootPassword) {
    throw "Cần -LoginPath (khuyến nghị) hoặc -RootPassword."
}

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd_HHmm"
$dumpFile = Join-Path $env:TEMP "${Database}_${stamp}.sql"
$zipFile = Join-Path $BackupDir "${Database}_${stamp}.zip"

$authArgs = if ($LoginPath) { @("--login-path=$LoginPath") } else { @("-u", "root", "--password=$RootPassword") }

Write-Host "=== Dump $Database ==="
& $MySqlDump @authArgs --single-transaction --routines --triggers --no-tablespaces `
    --default-character-set=utf8mb4 "--result-file=$dumpFile" $Database
if ($LASTEXITCODE -ne 0) { throw "mysqldump lỗi (exit $LASTEXITCODE)" }
if (-not (Test-Path -LiteralPath $dumpFile) -or (Get-Item -LiteralPath $dumpFile).Length -eq 0) {
    throw "File dump rỗng hoặc không tồn tại: $dumpFile"
}

Write-Host "=== Nén → $zipFile ==="
Compress-Archive -LiteralPath $dumpFile -DestinationPath $zipFile -Force
Remove-Item -LiteralPath $dumpFile -Force

Write-Host "=== Dọn backup cũ hơn $KeepDays ngày ==="
Get-ChildItem $BackupDir -Filter "${Database}_*.zip" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$KeepDays) } |
    Remove-Item -Force

Write-Host "XONG: $zipFile ($([math]::Round((Get-Item $zipFile).Length/1MB,1)) MB)"

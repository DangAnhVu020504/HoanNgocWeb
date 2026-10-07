# ============================================================
# backup-mysql.ps1 — Backup MySQL hằng đêm (Phase 9 — RUNBOOK §3.1)
# Usage: .\backup-mysql.ps1 -RootPassword "xxx" [-BackupDir "D:\Backup"] [-KeepDays 14]
# Task Scheduler: 23:30 hằng ngày — "powershell -File backup-mysql.ps1 ..."
# ============================================================
param(
    [Parameter(Mandatory = $true)][string]$RootPassword,
    [string]$BackupDir = "D:\Backup",
    [int]$KeepDays = 14,
    [string]$MySqlDump = "mysqldump",
    [string]$Database = "techposweb"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd_HHmm"
$dumpFile = Join-Path $env:TEMP "${Database}_${stamp}.sql"
$zipFile = Join-Path $BackupDir "${Database}_${stamp}.zip"

Write-Host "=== Dump $Database ==="
& $MySqlDump -u root "--password=$RootPassword" --single-transaction --routines --triggers $Database | Out-File -LiteralPath $dumpFile -Encoding UTF8
if ($LASTEXITCODE -ne 0) { throw "mysqldump lỗi" }

Write-Host "=== Nén → $zipFile ==="
Compress-Archive -LiteralPath $dumpFile -DestinationPath $zipFile -Force
Remove-Item -LiteralPath $dumpFile -Force

Write-Host "=== Dọn backup cũ hơn $KeepDays ngày ==="
Get-ChildItem $BackupDir -Filter "${Database}_*.zip" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$KeepDays) } |
    Remove-Item -Force

Write-Host "XONG: $zipFile ($([math]::Round((Get-Item $zipFile).Length/1MB,1)) MB)"

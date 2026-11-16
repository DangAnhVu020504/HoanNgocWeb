# ============================================================
# register-backup-task.ps1 — Đăng ký Task Scheduler backup MySQL 23:30 (RUNBOOK §3.1)
# Usage:
#   mysql_config_editor set --login-path=techpos-backup --host=localhost --user=root --password
#   .\register-backup-task.ps1 -LoginPath techpos-backup [-BackupDir "D:\Backup"] [-At "23:30"] [-RunNow]
# Chạy PowerShell với quyền Administrator.
# ============================================================
param(
    [string]$LoginPath,
    [string]$RootPassword,
    [string]$BackupDir = "D:\Backup",
    [int]$KeepDays = 14,
    [string]$At = "23:30",
    [string]$TaskName = "TechPos MySQL Backup",
    [string]$MySqlDump = "mysqldump",
    [string]$Database = "techposweb",
    [string]$TaskUser = "$env:USERDOMAIN\$env:USERNAME",
    [string]$TaskPassword,
    [switch]$RunAsSystem,
    [switch]$RunNow
)

$ErrorActionPreference = "Stop"
if (-not $LoginPath -and -not $RootPassword) {
    throw "Cần -LoginPath (khuyến nghị) hoặc -RootPassword."
}

$backupScript = Join-Path $PSScriptRoot "backup-mysql.ps1"
if (-not (Test-Path -LiteralPath $backupScript)) { throw "Không thấy $backupScript" }

$authArg = if ($LoginPath) { "-LoginPath `"$LoginPath`"" } else { "-RootPassword `"$RootPassword`"" }
$arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$backupScript`" $authArg " +
    "-BackupDir `"$BackupDir`" -KeepDays $KeepDays -MySqlDump `"$MySqlDump`" -Database `"$Database`""

$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $arguments
$trigger = New-ScheduledTaskTrigger -Daily -At $At
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopOnIdleEnd `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1) -MultipleInstances IgnoreNew

if ($RunAsSystem) {
    $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
} elseif ($TaskPassword) {
    $principal = $null
} else {
    $principal = New-ScheduledTaskPrincipal -UserId $TaskUser -LogonType S4U -RunLevel Highest
}

if ($TaskPassword -and -not $RunAsSystem) {
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
        -User $TaskUser -Password $TaskPassword -RunLevel Highest -Force | Out-Null
} else {
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
        -Principal $principal -Force | Out-Null
}

Write-Host "Đã đăng ký task '$TaskName' — hằng ngày lúc $At, backup vào $BackupDir (giữ $KeepDays ngày)."
Get-ScheduledTask -TaskName $TaskName | Select-Object TaskName, State | Format-Table

if ($RunNow) {
    Write-Host "=== Chạy thử ngay ==="
    Start-ScheduledTask -TaskName $TaskName
    Start-Sleep -Seconds 5
    Get-ScheduledTaskInfo -TaskName $TaskName | Select-Object LastRunTime, LastTaskResult | Format-Table
}

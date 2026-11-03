# Offline compile check: opens the project in batch mode, compiles every assembly, then
# reports any compiler errors/warnings found in the log. Exit code 0 = clean.
# Usage: powershell -File Tools/compile-check.ps1 [-ProjectPath <path>]
param([string]$ProjectPath = (Resolve-Path "$PSScriptRoot/..").Path)

. "$PSScriptRoot/UnityPath.ps1"
Assert-KitProjectClosed $ProjectPath
$unity = Get-KitUnityExe $ProjectPath
$log = Join-Path $ProjectPath "Logs/compile-check.log"
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null

$p = Start-Process -FilePath $unity -ArgumentList @("-batchmode", "-nographics", "-quit", "-projectPath", "`"$ProjectPath`"", "-logFile", "`"$log`"") -Wait -PassThru
$errors = Select-String -Path $log -Pattern "error CS\d+" | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
$warnings = Select-String -Path $log -Pattern "warning CS\d+" | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique

$warnings | ForEach-Object { Write-Host "WARN  $_" }
$errors | ForEach-Object { Write-Host "ERROR $_" }
Write-Host "Unity exit code $($p.ExitCode); $($errors.Count) error(s), $($warnings.Count) warning(s). Log: $log"
if ($p.ExitCode -ne 0 -or $errors.Count -gt 0) { exit 1 }
exit 0

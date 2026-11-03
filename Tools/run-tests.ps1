# Batch-mode test runner: runs EditMode then PlayMode tests and prints a summary per platform.
# Exit code 0 = all passed. Results land in TestResults/ (gitignored).
# Usage: powershell -File Tools/run-tests.ps1 [-ProjectPath <path>] [-Platform EditMode|PlayMode|All]
param(
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot/..").Path,
    [ValidateSet("EditMode", "PlayMode", "All")][string]$Platform = "All"
)

. "$PSScriptRoot/UnityPath.ps1"
Assert-KitProjectClosed $ProjectPath
$unity = Get-KitUnityExe $ProjectPath
$outDir = Join-Path $ProjectPath "TestResults"
New-Item -ItemType Directory -Force $outDir | Out-Null

$platforms = if ($Platform -eq "All") { @("EditMode", "PlayMode") } else { @($Platform) }
$failed = $false
foreach ($pf in $platforms) {
    $xml = Join-Path $outDir "results-$pf.xml"
    $log = Join-Path $outDir "log-$pf.txt"
    if (Test-Path $xml) { Remove-Item $xml }
    $p = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
        "-batchmode", "-runTests", "-testPlatform", $pf,
        "-projectPath", "`"$ProjectPath`"", "-testResults", "`"$xml`"", "-logFile", "`"$log`"")
    if (-not (Test-Path $xml)) { Write-Host "$pf : no results file (exit $($p.ExitCode)). See $log"; $failed = $true; continue }
    $run = ([xml](Get-Content $xml)).'test-run'
    Write-Host "$pf : total $($run.total), passed $($run.passed), failed $($run.failed), skipped $($run.skipped) -> $($run.result)"
    if ([int]$run.failed -gt 0 -or $p.ExitCode -ne 0) { $failed = $true }
}
if ($failed) { exit 1 }
exit 0

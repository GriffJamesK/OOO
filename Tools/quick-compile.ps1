# Fast offline compile check that works while the Editor is open and from any git worktree.
# It reuses the compiler response files Unity's last build left in the main project's Library
# (references, defines, language version), swaps in the source files from -SourceRoot, and runs
# Unity's bundled Roslyn on every Kit assembly in dependency order. It takes seconds, needs no
# Unity process, and doesn't touch the project. It can't see assets, so it is not a substitute
# for the integrator's compile in the Editor, but it catches every C# error and warning.
# Usage: powershell -File Tools/quick-compile.ps1 [-SourceRoot <worktree>] [-LibraryRoot <main project>]
# Exit code 0 = clean (no errors, no warnings).
param(
    [string]$SourceRoot = (Resolve-Path "$PSScriptRoot/..").Path,
    [string]$LibraryRoot = "C:\Unity Projects\OOO"
)

$ErrorActionPreference = "Stop"
$unityData = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data"
$dotnet = Join-Path $unityData "DotNetSdk\dotnet.exe"
$csc = Get-ChildItem (Join-Path $unityData "DotNetSdk\sdk") -Recurse -Filter csc.dll | Where-Object { $_.FullName -like "*bincore*" } | Select-Object -First 1
if (-not $csc) { Write-Host "ERROR csc.dll not found under $unityData"; exit 2 }

$dag = Get-ChildItem (Join-Path $LibraryRoot "Library\Bee\artifacts") -Directory -Filter "*.dag" |
    Where-Object { Test-Path (Join-Path $_.FullName "Kit.Core.rsp") } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $dag) { Write-Host "ERROR no Unity build artifacts in $LibraryRoot\Library (open the project in Unity once)"; exit 2 }

$out = Join-Path ([System.IO.Path]::GetTempPath()) ("kit-qc-" + [Guid]::NewGuid().ToString("N").Substring(0, 8))
New-Item -ItemType Directory -Force $out | Out-Null

# Dependency order: each assembly may reference only those before it.
$assemblies = @("Kit.Core", "Kit.Input", "Kit.UI", "Kit.Gameplay", "Kit.Editor", "Kit.Tests.EditMode", "Kit.Tests.PlayMode")
$built = @{}
$totalErrors = 0; $totalWarnings = 0

foreach ($asm in $assemblies) {
    $asmdef = Get-ChildItem (Join-Path $SourceRoot "Assets\_Project") -Recurse -Filter "$asm.asmdef" | Select-Object -First 1
    if (-not $asmdef) { Write-Host "SKIP  $asm (no asmdef in source)"; continue }
    $rspIn = Join-Path $dag.FullName "$asm.rsp"
    if (-not (Test-Path $rspIn)) { Write-Host "SKIP  $asm (no response file; compile once in the Editor)"; continue }

    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($line in Get-Content $rspIn) {
        if ($line -match '^"Assets/.*\.cs"$') { continue }                     # old source list
        if ($line -match '^-(out|refout|analyzer):' -or $line -match '^/additionalfile:') { continue }
        if ($line -match '^-r:"Library/Bee/artifacts/[^/]+/(Kit\.[A-Za-z.]+)\.ref\.dll"$') {
            $dep = $Matches[1]
            if ($built.ContainsKey($dep)) { $lines.Add("-r:`"$($built[$dep])`"") } else { $lines.Add(($line -replace '^-r:"Library/', "-r:`"$LibraryRoot/Library/")) }
            continue
        }
        $lines.Add(($line -replace '^-r:"Library/', "-r:`"$LibraryRoot/Library/"))
    }
    $dll = Join-Path $out "$asm.dll"
    $lines.Add("-out:`"$dll`"")
    Get-ChildItem $asmdef.DirectoryName -Recurse -Filter *.cs | ForEach-Object { $lines.Add("`"$($_.FullName)`"") }

    $rsp = Join-Path $out "$asm.rsp"
    Set-Content -Path $rsp -Value $lines -Encoding UTF8
    $result = & $dotnet $csc.FullName "@$rsp" 2>&1 | Out-String
    $errs = @($result -split "`r?`n" | Where-Object { $_ -match "error CS\d+" } | Sort-Object -Unique)
    $warns = @($result -split "`r?`n" | Where-Object { $_ -match "warning CS\d+" } | Sort-Object -Unique)
    $warns | ForEach-Object { Write-Host "WARN  $_" }
    $errs | ForEach-Object { Write-Host "ERROR $_" }
    $totalErrors += $errs.Count; $totalWarnings += $warns.Count
    if ($errs.Count -eq 0 -and (Test-Path $dll)) { $built[$asm] = $dll; Write-Host "OK    $asm" }
    else { Write-Host "FAIL  $asm"; break }
}

Remove-Item -Recurse -Force $out -ErrorAction SilentlyContinue
Write-Host "quick-compile: $totalErrors error(s), $totalWarnings warning(s)"
if ($totalErrors -gt 0 -or $totalWarnings -gt 0) { exit 1 }
exit 0

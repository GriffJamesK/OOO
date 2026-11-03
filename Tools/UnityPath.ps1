# Shared helper: resolves the Unity.exe that matches ProjectSettings/ProjectVersion.txt.
# Override with $env:UNITY_EXE when Unity Hub installs elsewhere.
function Get-KitUnityExe([string]$ProjectPath) {
    if ($env:UNITY_EXE -and (Test-Path $env:UNITY_EXE)) { return $env:UNITY_EXE }
    $versionLine = Get-Content (Join-Path $ProjectPath "ProjectSettings/ProjectVersion.txt") | Where-Object { $_ -like "m_EditorVersion:*" }
    $version = ($versionLine -split ":\s*")[1].Trim()
    $exe = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path $exe)) { throw "Unity $version not found at $exe. Install it with Unity Hub or set UNITY_EXE." }
    return $exe
}

function Assert-KitProjectClosed([string]$ProjectPath) {
    # Unity holds Temp/UnityLockfile open while the Editor has the project.
    $lock = Join-Path $ProjectPath "Temp/UnityLockfile"
    if (Test-Path $lock) {
        try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() }
        catch { throw "The project is open in a Unity Editor. Close it, or run this against a fresh clone (see CLAUDE.md)." }
    }
}

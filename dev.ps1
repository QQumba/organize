# Runs a throwaway Core and tray app from source with data in .dev/ (git-ignored). Safe next to an installed copy:
# own port (47822), hotkey (Alt+Shift+Q) and data. Ctrl+C stops both.
# Use the dev Core from the CLI with: org --config .dev/cli.json <command>
# -ConfigOnly [-ConfigDir <dir>] only creates the config files (used by the tests).
param([switch]$ConfigOnly, [string]$ConfigDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts/config.ps1')

$devPort = 47822
if (-not $ConfigDir) { $ConfigDir = Join-Path $PSScriptRoot '.dev' }
Initialize-OrganizerConfig -ConfigDir $ConfigDir -Port $devPort -Dev -Hotkey 'Alt+Shift+Q'
if ($ConfigOnly) { return }

Push-Location $PSScriptRoot
$core = $null
$desktop = $null
try {
    $core = Start-Process dotnet -ArgumentList 'run', '--project', 'core/Organizer.Core', '--launch-profile', 'Dev' -NoNewWindow -PassThru
    $desktop = Start-Process dotnet -ArgumentList 'run', '--project', 'desktop/Organizer.Desktop', '--', '--config', (Join-Path $ConfigDir 'desktop.json') -NoNewWindow -PassThru
    Write-Host "Dev Core on port $devPort, tray app on Alt+Shift+Q. Ctrl+C to stop."
    while (-not ($core.HasExited -and $desktop.HasExited)) { Start-Sleep -Seconds 1 }
}
finally {
    foreach ($p in $core, $desktop) {
        if ($p -and -not $p.HasExited) { taskkill /PID $p.Id /T /F | Out-Null }
    }
    Pop-Location
}

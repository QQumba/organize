# Runs a throwaway Core and tray app from source with data in .dev/ (git-ignored). Safe next to an installed copy:
# own port (47822), hotkey (Alt+Shift+Q) and data. Ctrl+C stops both.
# -Web also starts the web UI's Vite dev server (needs Node 24+; runs npm install first if needed). Vite prints its URL.
# Use the dev Core from the CLI with: org --config .dev/cli.json <command>
# -ConfigOnly [-ConfigDir <dir>] only creates the config files (used by the tests).
param([switch]$ConfigOnly, [switch]$Web, [string]$ConfigDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts/config.ps1')

$devPort = 47822
if (-not $ConfigDir) { $ConfigDir = Join-Path $PSScriptRoot '.dev' }
Initialize-OrganizerConfig -ConfigDir $ConfigDir -Port $devPort -Dev -Hotkey 'Alt+Shift+Q'
if ($ConfigOnly) { return }

if ($Web) {
    $nodeVersion = try { (& node --version) -replace '^v' } catch { $null }
    if (-not $nodeVersion -or [version]($nodeVersion -split '-')[0] -lt [version]'24.0') {
        throw "-Web needs Node 24 or later (found: $(if ($nodeVersion) { $nodeVersion } else { 'none' })). Install it from https://nodejs.org."
    }
}

Push-Location $PSScriptRoot
$core = $null
$desktop = $null
$vite = $null
try {
    $core = Start-Process dotnet -ArgumentList 'run', '--project', 'core/Organizer.Core', '--launch-profile', 'Dev' -NoNewWindow -PassThru
    $desktop = Start-Process dotnet -ArgumentList 'run', '--project', 'desktop/Organizer.Desktop', '--', '--config', (Join-Path $ConfigDir 'desktop.json') -NoNewWindow -PassThru
    if ($Web) {
        $webDir = Join-Path $PSScriptRoot 'web'
        if (-not (Test-Path (Join-Path $webDir 'node_modules'))) {
            Push-Location $webDir
            try { npm.cmd install; if ($LASTEXITCODE -ne 0) { throw 'npm install failed' } } finally { Pop-Location }
        }
        $vite = Start-Process npm.cmd -ArgumentList 'run', 'dev' -WorkingDirectory $webDir -NoNewWindow -PassThru
    }
    Write-Host "Dev Core on port $devPort, tray app on Alt+Shift+Q. Ctrl+C to stop."
    while (-not ($core.HasExited -and $desktop.HasExited -and (-not $vite -or $vite.HasExited))) { Start-Sleep -Seconds 1 }
}
finally {
    foreach ($p in $core, $desktop, $vite) {
        if ($p -and -not $p.HasExited) { taskkill /PID $p.Id /T /F | Out-Null }
    }
    Pop-Location
}

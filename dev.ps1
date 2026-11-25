# Runs a throwaway Core and tray app from source with data in .dev/ (git-ignored). Safe next to an installed copy:
# own port (47822), hotkey (Alt+Shift+Q) and data. Ctrl+C stops both.
# -Web also starts the web UI's Vite dev server (needs Node 24+; runs npm install first if needed). Vite prints its URL.
# Use the dev Core from the CLI with: org --config .dev/cli.json <command>
# -ConfigOnly [-ConfigDir <dir>] only creates the config files (used by the tests).
# -Start [-Web] [-NoTray] starts everything detached (hidden, logs in <ConfigDir>/logs), records the processes in
# <ConfigDir>/pids.json, waits until they answer and returns. -Stop stops exactly the recorded processes, and only
# those whose start time and path still match. Never by image name, port or pattern. For agents and scripts.
param([switch]$ConfigOnly, [switch]$Web, [string]$ConfigDir, [switch]$Start, [switch]$Stop, [switch]$NoTray)
$ErrorActionPreference = 'Stop'

if ($Start -and $Stop) { Write-Host '-Start and -Stop cannot be used together.'; exit 1 }
if (($Start -or $Stop) -and $ConfigOnly) { Write-Host '-ConfigOnly cannot be combined with -Start or -Stop.'; exit 1 }

$devPort = 47822
$vitePort = 5173
if (-not $ConfigDir) { $ConfigDir = Join-Path $PSScriptRoot '.dev' }
$pidFile = Join-Path $ConfigDir 'pids.json'

function ConvertTo-Ticks($value) {
    if ($value -is [datetime]) { return $value.ToUniversalTime().Ticks }
    [datetime]::Parse([string]$value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime().Ticks
}

# Stops the recorded set (only verified processes), deletes pids.json. Always returns normally.
function Stop-RecordedSet {
    $entries = $null
    if (Test-Path $pidFile) {
        try { $entries = Get-Content $pidFile -Raw | ConvertFrom-Json } catch { Write-Host "pids.json is unreadable, ignoring it." }
    }
    if ($entries) {
        foreach ($prop in $entries.PSObject.Properties) {
            $name = $prop.Name; $e = $prop.Value
            try {
                $proc = Get-Process -Id ([int]$e.pid) -ErrorAction SilentlyContinue
                if (-not $proc) { Write-Host "${name}: not running (pid $($e.pid))."; continue }
                $path = $null; $ticks = $null
                try { $path = $proc.Path; $ticks = $proc.StartTime.ToUniversalTime().Ticks } catch { }
                if (-not $path) { Write-Host "${name}: skipped pid $($e.pid), cannot read its path (elevated?)."; continue }
                if ($ticks -ne (ConvertTo-Ticks $e.startTime) -or $path -ne $e.path) {
                    Write-Host "${name}: skipped pid $($e.pid), it is not the process that was started (reused pid)."
                    continue
                }
                taskkill /PID $proc.Id /T /F | Out-Null
                Write-Host "${name}: stopped pid $($e.pid)."
            } catch { Write-Host "${name}: skipped, $($_.Exception.Message)" }
        }
    }
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
}

if ($Stop) { Stop-RecordedSet; exit 0 }

. (Join-Path $PSScriptRoot 'scripts/config.ps1')
Initialize-OrganizerConfig -ConfigDir $ConfigDir -Port $devPort -Dev -Hotkey 'Alt+Shift+Q'
if ($ConfigOnly) { return }

if ($Web) {
    $nodeVersion = try { (& node --version) -replace '^v' } catch { $null }
    if (-not $nodeVersion -or [version]($nodeVersion -split '-')[0] -lt [version]'24.0') {
        throw "-Web needs Node 24 or later (found: $(if ($nodeVersion) { $nodeVersion } else { 'none' })). Install it from https://nodejs.org."
    }
}

function Install-WebDependencies {
    $webDir = Join-Path $PSScriptRoot 'web'
    if (-not (Test-Path (Join-Path $webDir 'node_modules'))) {
        Push-Location $webDir
        try { npm.cmd install; if ($LASTEXITCODE -ne 0) { throw 'npm install failed' } } finally { Pop-Location }
    }
}

# Builds the projects one after the other, so two MSBuild processes never write the shared Contracts.dll at once
# (the cause of CS2012 when both `dotnet run` built in parallel). Quiet: output is shown only on failure.
# The projects are then started with `dotnet run --no-build`. Returns $true on success.
function Build-DevProjects {
    $projects = @('core/Organizer.Core') + $(if (-not $NoTray) { 'desktop/Organizer.Desktop' })
    foreach ($project in $projects) {
        $output = & dotnet build (Join-Path $PSScriptRoot $project) --nologo -v q 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build of $project failed:"
            $output | ForEach-Object { Write-Host $_ }
            return $false
        }
    }
    return $true
}

if ($Start) {
    function Test-PortListening([int]$port) {
        $null -ne (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue)
    }
    function Get-HttpStatus([string]$url) {
        try { [int](Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 5).StatusCode }
        catch { if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 } }
    }
    function Show-LogTail([string]$name) {
        foreach ($f in "$name.log") {
            $path = Join-Path $logDir $f
            if (Test-Path $path) { Write-Host "--- $f (last 20 lines)"; Get-Content $path -Tail 20 | ForEach-Object { Write-Host $_ } }
        }
    }

    Stop-RecordedSet

    $ports = @($devPort) + $(if ($Web) { $vitePort })
    foreach ($port in $ports) {
        $deadline = (Get-Date).AddSeconds(5)
        while ((Test-PortListening $port) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 250 }
        if (Test-PortListening $port) {
            Write-Host "Port $port is already in use by another process. Not starting; the other process was left alone."
            exit 1
        }
    }

    if (-not (Build-DevProjects)) { exit 1 }
    if ($Web) { Install-WebDependencies }
    $logDir = Join-Path $ConfigDir 'logs'
    New-Item -ItemType Directory -Force $logDir | Out-Null

    $started = [ordered]@{}   # name -> Process
    $record = [ordered]@{}
    # Started through WMI, not Start-Process: a child of Start-Process inherits this process's stdout handle (a caller's
    # pipe) and keeps it open after we exit, so a caller reading the pipe to its end would wait for the dev servers.
    # The root process is cmd.exe, which sends stdout and stderr of the real command to <name>.log.
    function Start-Detached([string]$name, [string]$file, [string[]]$arguments, [string]$workDir) {
        $out = Join-Path $logDir "$name.log"
        $command = (@($file) + $arguments | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' '
        $r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
            CommandLine      = "cmd.exe /d /c `"$command > `"$out`" 2>&1 < nul`""
            CurrentDirectory = $workDir
            ProcessStartupInformation = New-CimInstance -ClientOnly -ClassName Win32_ProcessStartup -Property @{ ShowWindow = [uint16]0 }
        }
        if ($r.ReturnValue -ne 0) { throw "Could not start ${name} (WMI error $($r.ReturnValue))." }
        $p = Get-Process -Id ([int]$r.ProcessId)
        $started[$name] = $p
        $live = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
        $record[$name] = [ordered]@{
            pid       = $p.Id
            startTime = $p.StartTime.ToUniversalTime().ToString('o')
            path      = if ($live) { $live.Path } else { $null }
        }
        # Written after every start so a failed or interrupted start can still be undone with -Stop.
        $record | ConvertTo-Json -Depth 4 | Set-Content $pidFile -Encoding UTF8
    }

    try {
        Start-Detached 'core' 'dotnet' @('run', '--no-build', '--project', 'core/Organizer.Core', '--launch-profile', 'Dev') $PSScriptRoot
        if (-not $NoTray) {
            Start-Detached 'desktop' 'dotnet' @('run', '--no-build', '--project', 'desktop/Organizer.Desktop', '--', '--config', (Join-Path $ConfigDir 'desktop.json')) $PSScriptRoot
        }
        if ($Web) {
            Start-Detached 'vite' 'npm.cmd' @('run', 'dev', '--', '--port', "$vitePort", '--strictPort') (Join-Path $PSScriptRoot 'web')
        }

        $checks = @(@{ Name = 'core'; Label = "Core health at http://127.0.0.1:$devPort/api/health"; Url = "http://127.0.0.1:$devPort/api/health"; Ok = { param($s) $s -gt 0 -and $s -lt 500 } })
        if ($Web) { $checks += @{ Name = 'vite'; Label = "Vite at http://localhost:$vitePort/"; Url = "http://localhost:$vitePort/"; Ok = { param($s) $s -eq 200 } } }

        $deadline = (Get-Date).AddSeconds(120)
        $pending = @($checks)
        while ($pending.Count -gt 0) {
            foreach ($name in $started.Keys) {
                if ($started[$name].HasExited) { throw "The $name process exited early (code $($started[$name].ExitCode))." }
            }
            if ((Get-Date) -gt $deadline) { throw "Timed out after 120 s waiting for: $($pending.Label -join '; ')." }
            $pending = @($pending | Where-Object { -not (& $_.Ok (Get-HttpStatus $_.Url)) })
            if ($pending.Count -gt 0) { Start-Sleep -Milliseconds 500 }
        }
    }
    catch {
        Write-Host "Start failed: $($_.Exception.Message)"
        foreach ($name in $started.Keys) { Show-LogTail $name }
        Stop-RecordedSet
        exit 1
    }

    Write-Host "Dev Core: http://127.0.0.1:$devPort"
    if ($Web) { Write-Host "Vite: http://localhost:$vitePort" }
    Write-Host "Stop with: powershell ./dev.ps1 -Stop"
    exit 0
}

Push-Location $PSScriptRoot
$core = $null
$desktop = $null
$vite = $null
try {
    if (-not (Build-DevProjects)) { exit 1 }
    $core = Start-Process dotnet -ArgumentList 'run', '--no-build', '--project', 'core/Organizer.Core', '--launch-profile', 'Dev' -NoNewWindow -PassThru
    if (-not $NoTray) {
        $desktop = Start-Process dotnet -ArgumentList 'run', '--no-build', '--project', 'desktop/Organizer.Desktop', '--', '--config', (Join-Path $ConfigDir 'desktop.json') -NoNewWindow -PassThru
    }
    if ($Web) {
        Install-WebDependencies
        $vite = Start-Process npm.cmd -ArgumentList 'run', 'dev' -WorkingDirectory (Join-Path $PSScriptRoot 'web') -NoNewWindow -PassThru
    }
    Write-Host "Dev Core on port $devPort, tray app on Alt+Shift+Q. Ctrl+C to stop."
    while (-not ($core.HasExited -and (-not $desktop -or $desktop.HasExited) -and (-not $vite -or $vite.HasExited))) { Start-Sleep -Seconds 1 }
}
finally {
    foreach ($p in $core, $desktop, $vite) {
        if ($p -and -not $p.HasExited) { taskkill /PID $p.Id /T /F | Out-Null }
    }
    Pop-Location
}

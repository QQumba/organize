# Per-user install: copies dist/ to %LOCALAPPDATA%\Organizer, registers the Core's logon task and the tray app's
# Run key, adds the CLI to the user PATH and creates missing config files. Re-running upgrades in place.
# Run `powershell ./publish.ps1` first, from a normal (non-elevated) PowerShell.
# -ConfigOnly -ConfigDir <dir> only creates the config files (used by the tests).
param([switch]$ConfigOnly, [string]$ConfigDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts/config.ps1')

if (-not $ConfigDir) { $ConfigDir = Join-Path $env:APPDATA 'Organizer' }

if ($ConfigOnly) {
    $port = Get-CorePort (Join-Path $PSScriptRoot 'core/Organizer.Core/appsettings.json')
    Initialize-OrganizerConfig -ConfigDir $ConfigDir -Port $port
    return
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
if (([Security.Principal.WindowsPrincipal]$identity).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    [Console]::Error.WriteLine("Run install.ps1 from a normal (non-elevated) PowerShell. Elevated, the per-user settings (task, Run key, PATH, config) would go to the wrong account.")
    exit 1
}

$taskName = 'Organizer Core'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$target = Join-Path $env:LOCALAPPDATA 'Organizer'
$coreExe = Join-Path $target 'core\Organizer.Core.exe'
$desktopExe = Join-Path $target 'desktop\Organizer.Desktop.exe'
$cliDir = Join-Path $target 'cli'

try {
    foreach ($p in @(
            @('core', 'Organizer.Core.exe'), @('cli', 'org.exe'), @('desktop', 'Organizer.Desktop.exe'))) {
        if (-not (Test-Path (Join-Path $PSScriptRoot "dist/$($p[0])/$($p[1])"))) {
            throw "dist/$($p[0])/$($p[1]) not found. Run 'powershell ./publish.ps1' first."
        }
    }
    $port = Get-CorePort (Join-Path $PSScriptRoot 'dist/core/appsettings.json')

    # Stop the running copy (task first, then the process; by path so a dev Core is left alone).
    if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) { Stop-ScheduledTask -TaskName $taskName }
    foreach ($exe in $coreExe, $desktopExe) {
        $name = [IO.Path]::GetFileNameWithoutExtension($exe)
        Get-Process -Name $name -ErrorAction SilentlyContinue |
            Where-Object { $_.Path -eq $exe } |
            ForEach-Object { $_.Kill(); $_.WaitForExit(10000) | Out-Null }
    }

    foreach ($part in 'core', 'cli', 'desktop') {
        $dest = Join-Path $target $part
        if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
        Copy-Item (Join-Path $PSScriptRoot "dist/$part/*") $dest -Recurse -Force
        Write-Host "Installed $dest"
    }

    Initialize-OrganizerConfig -ConfigDir $ConfigDir -Port $port

    # Core: per-user logon task, no admin, no stored password. The working directory is the content root
    # (appsettings.json is found there), so it must be the install folder.
    $user = $env:USERNAME
    $action = New-ScheduledTaskAction -Execute $coreExe -WorkingDirectory (Split-Path $coreExe)
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$user" -LogonType Interactive
    $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -MultipleInstances IgnoreNew
    try {
        Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal `
            -Settings $settings -Force | Out-Null
        Start-ScheduledTask -TaskName $taskName
    }
    catch {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
        throw
    }
    Write-Host "Registered and started task '$taskName'"

    # Tray app: starts at login through the Run key.
    Set-ItemProperty -Path $runKey -Name 'Organizer' -Value "`"$desktopExe`""
    Start-Process $desktopExe
    Write-Host "Tray app set to start at login and started"

    # CLI on the user PATH, once.
    $entries = @([Environment]::GetEnvironmentVariable('Path', 'User') -split ';' | Where-Object { $_ })
    if (-not ($entries | Where-Object { $_.TrimEnd('\') -ieq $cliDir })) {
        [Environment]::SetEnvironmentVariable('Path', (($entries + $cliDir) -join ';'), 'User')
        Write-Host "Added $cliDir to the user PATH (open a new terminal to use 'org')"
    }
    else { Write-Host "$cliDir is already on the user PATH" }

    Write-Host "Done. Config and data: $ConfigDir"
}
catch {
    [Console]::Error.WriteLine("Install failed: $($_.Exception.Message)")
    exit 1
}

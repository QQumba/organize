# Removes what install.ps1 set up. Keeps config and data in %APPDATA%\Organizer unless -RemoveData is passed.
param([switch]$RemoveData)
$ErrorActionPreference = 'Stop'

$taskName = 'Organizer Core'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$target = Join-Path $env:LOCALAPPDATA 'Organizer'
$cliDir = Join-Path $target 'cli'

try {
    if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
        Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "Removed task '$taskName'"
    }
    foreach ($exe in 'core\Organizer.Core.exe', 'desktop\Organizer.Desktop.exe') {
        $path = Join-Path $target $exe
        Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($path)) -ErrorAction SilentlyContinue |
            Where-Object { $_.Path -eq $path } |
            ForEach-Object { $_.Kill(); $_.WaitForExit(10000) | Out-Null }
    }
    if (Get-ItemProperty -Path $runKey -Name 'Organizer' -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $runKey -Name 'Organizer'
        Write-Host "Removed Run value 'Organizer'"
    }
    $entries = @([Environment]::GetEnvironmentVariable('Path', 'User') -split ';' | Where-Object { $_ })
    $kept = @($entries | Where-Object { $_.TrimEnd('\') -ine $cliDir })
    if ($kept.Count -ne $entries.Count) {
        [Environment]::SetEnvironmentVariable('Path', ($kept -join ';'), 'User')
        Write-Host "Removed $cliDir from the user PATH"
    }
    if (Test-Path $target) { Remove-Item $target -Recurse -Force; Write-Host "Deleted $target" }

    $data = Join-Path $env:APPDATA 'Organizer'
    if ($RemoveData) {
        if (Test-Path $data) { Remove-Item $data -Recurse -Force; Write-Host "Deleted $data" }
    }
    else { Write-Host "Kept config and data in $data (use -RemoveData to delete)" }
}
catch {
    [Console]::Error.WriteLine("Uninstall failed: $($_.Exception.Message)")
    exit 1
}

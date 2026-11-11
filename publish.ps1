# Builds the deliverables into dist/ (git-ignored): core, cli, desktop. Grows as other deliverables appear.
# Prerequisites to run the result: ASP.NET Core 10 runtime (Core), .NET 10 Desktop Runtime (tray app).
# The CLI needs the Visual Studio "Desktop development with C++" workload for Native AOT.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'

# Core: framework-dependent, WinExe (no console window when started by the logon task). Needs the ASP.NET Core 10 runtime.
$coreOut = Join-Path $dist 'core'
if (Test-Path $coreOut) { Remove-Item $coreOut -Recurse -Force }
dotnet publish (Join-Path $root 'core/Organizer.Core') -c Release -r win-x64 --self-contained false -p:CoreWinExe=true -p:DebugType=none -p:DebugSymbols=false -o $coreOut
if ($LASTEXITCODE -ne 0) { throw "Core publish failed" }

Get-ChildItem $coreOut -Filter *.pdb | Remove-Item
Write-Host "Core: $coreOut\Organizer.Core.exe"

# CLI: Native AOT single exe (org.exe)
$cliOut = Join-Path $dist 'cli'
if (Test-Path $cliOut) { Remove-Item $cliOut -Recurse -Force }
dotnet publish (Join-Path $root 'cli/Organizer.Cli') -c Release -r win-x64 -p:DebugType=none -p:DebugSymbols=false -o $cliOut
if ($LASTEXITCODE -ne 0) { throw "CLI publish failed" }

Get-ChildItem $cliOut -Filter *.pdb | Remove-Item
Write-Host "CLI: $cliOut\org.exe"

# Desktop (tray app): framework-dependent, ReadyToRun (WPF does not support Native AOT). Needs the .NET 10 Desktop Runtime.
$desktopOut = Join-Path $dist 'desktop'
if (Test-Path $desktopOut) { Remove-Item $desktopOut -Recurse -Force }
dotnet publish (Join-Path $root 'desktop/Organizer.Desktop') -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=true -p:DebugType=none -p:DebugSymbols=false -o $desktopOut
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed" }

Get-ChildItem $desktopOut -Filter *.pdb | Remove-Item
Write-Host "Desktop: $desktopOut\Organizer.Desktop.exe"

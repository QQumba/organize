# Builds the deliverables into dist/ (git-ignored). Grows as other deliverables appear.
# The CLI needs the Visual Studio "Desktop development with C++" workload for Native AOT.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'

# The Native AOT linker step runs vswhere.exe, which the Visual Studio installer does not put on PATH.
$installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if ((Test-Path $installer) -and ($env:PATH -notlike "*$installer*")) { $env:PATH = "$installer;$env:PATH" }

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

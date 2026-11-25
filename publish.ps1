# Builds the deliverables into dist/ (git-ignored): core (with the web UI in wwwroot), cli, desktop. Grows as other deliverables appear.
# Prerequisites to run the result: ASP.NET Core 10 runtime (Core), .NET 10 Desktop Runtime (tray app).
# Node 24+ is needed to build the web UI.
# The CLI needs the Visual Studio "Desktop development with C++" workload for Native AOT.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# The Native AOT build (CLI) finds Visual Studio through vswhere.exe; outside a Developer prompt it is not on PATH
# and the link can fail. Add its fixed install folder for this process only.
$vswhereDir = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (-not (Get-Command vswhere.exe -ErrorAction SilentlyContinue) -and (Test-Path (Join-Path $vswhereDir 'vswhere.exe'))) {
    $env:PATH = "$env:PATH;$vswhereDir"
}
$dist = Join-Path $root 'dist'

# Node first, before any other step.
$nodeVersion = $null
try { $nodeVersion = (& node --version) } catch { }
if (-not $nodeVersion -or $nodeVersion -notmatch '^v(\d+)\.' -or [int]$Matches[1] -lt 24) {
    throw "Node 24 or later is needed to build the web UI"
}

# Web UI: built before the Core publish so a broken web build fails early.
$webDir = Join-Path $root 'web'
$webBuild = Join-Path $webDir 'build'
Push-Location $webDir
try {
    foreach ($npmArgs in @(@('ci'), @('run', 'check'), @('run', 'build'))) {
        & npm.cmd @npmArgs
        if ($LASTEXITCODE -ne 0) { throw "npm $($npmArgs -join ' ') failed" }
    }
}
finally { Pop-Location }
if (-not (Test-Path (Join-Path $webBuild 'index.html'))) { throw "Web build failed: web/build/index.html not found" }

# Gzipped size of the JS and CSS (soft budgets: warn, never fail).
function Get-GzipKB([string]$ext) {
    $total = 0
    foreach ($f in Get-ChildItem $webBuild -Recurse -File -Filter "*.$ext") {
        $ms = New-Object IO.MemoryStream
        $gz = New-Object IO.Compression.GZipStream($ms, [IO.Compression.CompressionLevel]::Optimal, $true)
        $bytes = [IO.File]::ReadAllBytes($f.FullName)
        $gz.Write($bytes, 0, $bytes.Length)
        $gz.Dispose()
        $total += $ms.Length
        $ms.Dispose()
    }
    return $total / 1024
}
$jsKB = Get-GzipKB 'js'
$cssKB = Get-GzipKB 'css'
Write-Host ("Web UI gzipped: JS {0:N1} KB (budget 80), CSS {1:N1} KB (budget 10)" -f $jsKB, $cssKB)
if ($jsKB -gt 80) { Write-Host "WARNING: web UI JS is over its 80 KB gzipped budget" -ForegroundColor Yellow }
if ($cssKB -gt 10) { Write-Host "WARNING: web UI CSS is over its 10 KB gzipped budget" -ForegroundColor Yellow }

# Core: framework-dependent, WinExe (no console window when started by the logon task). Needs the ASP.NET Core 10 runtime.
$coreOut = Join-Path $dist 'core'
if (Test-Path $coreOut) { Remove-Item $coreOut -Recurse -Force }
dotnet publish (Join-Path $root 'core/Organizer.Core') -c Release -r win-x64 --self-contained false -p:CoreWinExe=true -p:DebugType=none -p:DebugSymbols=false -o $coreOut
if ($LASTEXITCODE -ne 0) { throw "Core publish failed" }

Get-ChildItem $coreOut -Filter *.pdb | Remove-Item
Copy-Item $webBuild (Join-Path $coreOut 'wwwroot') -Recurse
Write-Host "Core: $coreOut\Organizer.Core.exe (web UI in wwwroot)"

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

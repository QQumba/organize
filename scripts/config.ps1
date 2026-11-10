# Shared by install.ps1 and dev.ps1 (dot-sourced). Creates the Core and Client config files.

function Get-CorePort([string]$AppSettingsPath) {
    $port = (Get-Content -LiteralPath $AppSettingsPath -Raw | ConvertFrom-Json).Port
    if (-not $port) { throw "No Port in $AppSettingsPath" }
    return [int]$port
}

function New-OrganizerSecret {
    # 32 random bytes, URL-safe base64 (same form as CoreConfigLoader.NewSecret).
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Read-JsonFile([string]$Path) {
    $o = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -eq $o) { $o = [pscustomobject]@{} }
    return $o
}

function Write-JsonFile([string]$Path, $Object) {
    $dir = Split-Path $Path -Parent
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
    $json = $Object | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText($Path, $json + "`n", (New-Object Text.UTF8Encoding($false)))
}

function Set-JsonKey($Object, [string]$Name, $Value) {
    if ($null -eq $Object.PSObject.Properties[$Name]) { $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value }
    else { $Object.$Name = $Value }
}

# Creates core.json, cli.json and desktop.json in $ConfigDir. Files that exist are never overwritten:
# one shared secret (the existing core.json secret wins) goes into every file that has to be created.
# -Dev also adds the dev Port and Hotkey keys when missing and fixes the Client Address port of existing files.
function Initialize-OrganizerConfig(
    [string]$ConfigDir, [int]$Port, [switch]$Dev, [string]$Hotkey = 'Alt+Shift+Q'
) {
    $address = "http://127.0.0.1:$Port"
    $corePath = Join-Path $ConfigDir 'core.json'

    if (Test-Path -LiteralPath $corePath) {
        $core = Read-JsonFile $corePath
        $secret = $core.Secret
        $dirty = $false
        if ([string]::IsNullOrWhiteSpace($secret)) {
            $secret = New-OrganizerSecret
            Set-JsonKey $core 'Secret' $secret
            $dirty = $true
            Write-Host "core.json had no Secret; added one: $corePath"
        }
        if ($Dev -and $null -eq $core.PSObject.Properties['Port']) {
            Set-JsonKey $core 'Port' $Port
            $dirty = $true
            Write-Host "Added Port $Port to $corePath"
        }
        if ($dirty) { Write-JsonFile $corePath $core } else { Write-Host "Kept $corePath" }
    }
    else {
        $secret = New-OrganizerSecret
        $core = [ordered]@{ Secret = $secret }
        if ($Dev) { $core.Port = $Port }
        Write-JsonFile $corePath ([pscustomobject]$core)
        Write-Host "Created $corePath"
    }

    foreach ($name in 'cli.json', 'desktop.json') {
        $path = Join-Path $ConfigDir $name
        if (-not (Test-Path -LiteralPath $path)) {
            $cfg = [ordered]@{ Address = $address; Secret = $secret }
            if ($Dev -and $name -eq 'desktop.json') { $cfg.Hotkey = $Hotkey }
            Write-JsonFile $path ([pscustomobject]$cfg)
            Write-Host "Created $path"
            continue
        }
        $cfg = Read-JsonFile $path
        $dirty = $false
        if ($cfg.Secret -ne $secret) {
            Write-Warning "$path has a different Secret than core.json; left alone. Fix it by hand if the Client cannot connect."
        }
        if ($Dev) {
            if ($cfg.Address -ne $address) { Set-JsonKey $cfg 'Address' $address; $dirty = $true }
            if ($name -eq 'desktop.json' -and $null -eq $cfg.PSObject.Properties['Hotkey']) {
                Set-JsonKey $cfg 'Hotkey' $Hotkey; $dirty = $true
            }
        }
        if ($dirty) { Write-JsonFile $path $cfg; Write-Host "Updated $path" } else { Write-Host "Kept $path" }
    }
}

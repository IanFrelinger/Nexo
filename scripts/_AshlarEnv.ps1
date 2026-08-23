# Shared .env helpers for Ashlar agent-server scripts (ASCII only).
# Dot-source:  . "$PSScriptRoot\_AshlarEnv.ps1"

function Get-AshlarRepoRoot {
    Resolve-Path (Join-Path $PSScriptRoot "..")
}

function Read-AshlarDotEnv {
    param([string]$Path)
    $map = @{}
    if (-not (Test-Path $Path)) { return $map }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#")) { return }
        $eq = $line.IndexOf("=")
        if ($eq -lt 1) { return }
        $key = $line.Substring(0, $eq).Trim()
        $val = $line.Substring($eq + 1).Trim()
        if (($val.StartsWith('"') -and $val.EndsWith('"')) -or ($val.StartsWith("'") -and $val.EndsWith("'"))) {
            $val = $val.Substring(1, $val.Length - 2)
        }
        $map[$key] = $val
    }
    return $map
}

function Set-AshlarDotEnvValue {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Value
    )
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $lines = @()
    if (Test-Path $Path) {
        $lines = @(Get-Content $Path)
    }

    $found = $false
    $out = foreach ($line in $lines) {
        if ($line -match "^\s*$([regex]::Escape($Key))\s*=") {
            $found = $true
            "$Key=$Value"
        } else {
            $line
        }
    }
    if (-not $found) {
        $out = @($out) + "$Key=$Value"
    }

    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($Path, $out, $utf8)
}

function Ensure-AshlarDotEnvFile {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [hashtable]$Defaults
    )
    $envPath = Join-Path $RepoRoot ".env"
    $example = Join-Path $RepoRoot "docs\config\agent-server.env.example"
    if (-not (Test-Path $envPath)) {
        if (Test-Path $example) {
            Copy-Item $example $envPath
        } else {
            New-Item -ItemType File -Path $envPath -Force | Out-Null
        }
        foreach ($k in $Defaults.Keys) {
            Set-AshlarDotEnvValue -Path $envPath -Key $k -Value ([string]$Defaults[$k])
        }
        Write-Host "Wrote $envPath"
    }
    return $envPath
}

function Get-AshlarEnvInt {
    param(
        [hashtable]$Map,
        [string]$Key,
        [int]$Default
    )
    if ($Map.ContainsKey($Key) -and $Map[$Key] -match '^\d+$') {
        return [int]$Map[$Key]
    }
    return $Default
}

function Get-AshlarApiBaseUrl {
    param(
        [string]$HostName = "127.0.0.1",
        [int]$Port = 8088,
        [string]$Scheme = "http"
    )
    return "${Scheme}://${HostName}:${Port}"
}

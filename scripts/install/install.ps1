[CmdletBinding()]
param(
    [string]$RepoUrl = "https://github.com/IanFrelinger/Nexo.git",
    [string]$InstallDir = "$HOME\Nexo",
    [string]$Branch,
    [switch]$Yes,
    [switch]$SkipBuild,
    [switch]$StartDaemon,
    [string]$DaemonDuration,
    [switch]$Hero,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Step {
    param([Parameter(Mandatory = $true)][string]$Command)
    if ($DryRun.IsPresent) {
        Write-Host "[dry-run] $Command"
        return
    }

    $global:LASTEXITCODE = 0
    Invoke-Expression $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed (exit $LASTEXITCODE): $Command"
    }
}

function Get-DotnetMajor {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        return 0
    }
    $version = (& dotnet --version) 2>$null
    if ([string]::IsNullOrWhiteSpace($version)) {
        return 0
    }
    return [int]($version.Split('.')[0])
}

function Ensure-Dotnet {
    $major = Get-DotnetMajor
    if ($major -ge 9) {
        return
    }

    throw ".NET SDK 9+ is required. Install/update it via your IDE, then re-run this installer."
}

function Ensure-Windows {
    if (-not $env:OS -or $env:OS -ne "Windows_NT") {
        throw "This installer only supports Windows hosts."
    }
}

function Sync-Repo {
    param([Parameter(Mandatory = $true)][string]$TargetDir)

    if (Test-Path (Join-Path $TargetDir ".git")) {
        Invoke-Step "git -C \"$TargetDir\" fetch --all --tags"
        if (-not [string]::IsNullOrWhiteSpace($Branch)) {
            Invoke-Step "git -C \"$TargetDir\" checkout $Branch"
            Invoke-Step "git -C \"$TargetDir\" pull --ff-only origin $Branch"
        }
        else {
            Invoke-Step "git -C \"$TargetDir\" pull --ff-only"
        }
        return
    }

    $parent = Split-Path -Parent $TargetDir
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        if ($DryRun.IsPresent) {
            Write-Host "[dry-run] New-Item -ItemType Directory -Force -Path \"$parent\""
        }
        else {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($Branch)) {
        Invoke-Step "git clone --branch $Branch --single-branch \"$RepoUrl\" \"$TargetDir\""
    }
    else {
        Invoke-Step "git clone \"$RepoUrl\" \"$TargetDir\""
    }
}

function Run-Setup {
    param([Parameter(Mandatory = $true)][string]$TargetDir)

    Invoke-Step "Set-Location \"$TargetDir\"; powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode restore"
}

function Run-HeroFlow {
    param([Parameter(Mandatory = $true)][string]$TargetDir)

    if (-not $Hero.IsPresent) {
        return
    }

    if ($DryRun.IsPresent) {
        Write-Host "[dry-run] Set-Location ""$TargetDir""; dotnet run --project src\Nexo.CLI -- --help"
        Write-Host "[dry-run] Set-Location ""$TargetDir""; dotnet run --project src\Nexo.CLI -- doctor --json"
        Write-Host "[dry-run] create quickstart template and run pipeline validate/run/diagnostics"
        return
    }

    $tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) ("nexo-hero-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null
    try {
        $templatePath = Join-Path $tmpDir "nexo_quickstart.json"
        @'
{
  "templateId": "quickstart",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
'@ | Set-Content -Path $templatePath -NoNewline

        $runId = "hero-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
        Invoke-Step "Set-Location ""$TargetDir""; dotnet run --project src\Nexo.CLI -- --help"
        Invoke-Step "Set-Location ""$TargetDir""; dotnet run --project src\Nexo.CLI -- doctor --json"
        Invoke-Step "Set-Location ""$TargetDir""; dotnet run --project src\Nexo.CLI -- pipeline validate --template ""$templatePath"""
        Invoke-Step "Set-Location ""$TargetDir""; dotnet run --project src\Nexo.CLI -- pipeline run --template ""$templatePath"" --run-id $runId --format-json"
        Invoke-Step "Set-Location ""$TargetDir""; dotnet run --project src\Nexo.CLI -- pipeline diagnostics --format-json"
    }
    finally {
        Remove-Item -Path $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Run-Build {
    param([Parameter(Mandatory = $true)][string]$TargetDir)

    if ($SkipBuild.IsPresent) {
        return
    }

    Invoke-Step "Set-Location \"$TargetDir\"; dotnet build src\Nexo.CLI\Nexo.CLI.csproj --no-restore"
}

function Run-Daemon {
    param([Parameter(Mandatory = $true)][string]$TargetDir)

    if (-not $StartDaemon.IsPresent) {
        return
    }

    $daemonCmd = "Set-Location \"$TargetDir\"; dotnet run --project src\Nexo.CLI -- background-agent daemon"
    if (-not [string]::IsNullOrWhiteSpace($DaemonDuration)) {
        $daemonCmd += " --duration $DaemonDuration"
    }

    Invoke-Step $daemonCmd
}

function Print-NextSteps {
    param([Parameter(Mandatory = $true)][string]$TargetDir)

    Write-Host ""
    Write-Host "Install complete."
    Write-Host "Repo: $TargetDir"
    Write-Host ""
    Write-Host "Next commands:"
    Write-Host "  Set-Location \"$TargetDir\""
    Write-Host "  dotnet run --project src\Nexo.CLI -- --help"
    Write-Host "  dotnet run --project src\Nexo.CLI -- background-agent daemon --duration 30s"
}

Ensure-Windows
Ensure-Dotnet

$expandedInstallDir = [Environment]::ExpandEnvironmentVariables($InstallDir)
if ($expandedInstallDir.StartsWith('~')) {
    $expandedInstallDir = Join-Path $HOME $expandedInstallDir.Substring(1).TrimStart('\\', '/')
}

Write-Host "Nexo Windows installer"
Write-Host "  repo-url: $RepoUrl"
Write-Host "  install-dir: $expandedInstallDir"
if (-not [string]::IsNullOrWhiteSpace($Branch)) {
    Write-Host "  branch: $Branch"
}

Sync-Repo -TargetDir $expandedInstallDir
Run-Setup -TargetDir $expandedInstallDir
Run-Build -TargetDir $expandedInstallDir
Run-HeroFlow -TargetDir $expandedInstallDir
Run-Daemon -TargetDir $expandedInstallDir
Print-NextSteps -TargetDir $expandedInstallDir

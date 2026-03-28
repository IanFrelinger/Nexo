[CmdletBinding()]
param(
    [string]$RepoUrl = "https://github.com/IanFrelinger/Nexo.git",
    [string]$InstallDir = "$HOME\Nexo",
    [string]$Branch,
    [switch]$IncludeOptional,
    [switch]$Yes,
    [switch]$SkipBuild,
    [switch]$StartDaemon,
    [string]$DaemonDuration,
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

function Install-DotnetWithWinget {
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        throw "winget is required to install .NET SDK automatically."
    }
    Invoke-Step "winget install --id Microsoft.DotNet.SDK.9 --exact --accept-package-agreements --accept-source-agreements --silent"
}

function Ensure-Dotnet {
    $major = Get-DotnetMajor
    if ($major -ge 9) {
        return
    }

    Write-Host ".NET SDK 9+ not found. Installing..."
    Install-DotnetWithWinget

    if ($DryRun.IsPresent) {
        return
    }

    $major = Get-DotnetMajor
    if ($major -lt 9) {
        throw ".NET SDK 9+ installation did not complete successfully."
    }
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

    $setupArgs = "-Mode apply"
    if ($IncludeOptional.IsPresent) { $setupArgs += " -IncludeOptional" }
    if ($Yes.IsPresent) { $setupArgs += " -Yes" }

    Invoke-Step "Set-Location \"$TargetDir\"; powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 $setupArgs"
    Invoke-Step "Set-Location \"$TargetDir\"; powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode restore"
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
Run-Daemon -TargetDir $expandedInstallDir
Print-NextSteps -TargetDir $expandedInstallDir

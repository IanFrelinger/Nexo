[CmdletBinding()]
param(
    [string]$Image = "ghcr.io/ianfrelinger/nexo-cli:latest",
    [string]$SdkImage = "mcr.microsoft.com/dotnet/sdk:9.0",
    [string]$Workspace,
    [string]$StartDaemonDuration,
    [switch]$Guided,
    [switch]$WithSdk,
    [switch]$Yes,
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

function Ensure-Windows {
    if (-not $env:OS -or $env:OS -ne "Windows_NT") {
        throw "This script only supports Windows hosts."
    }
}

function Test-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-WingetPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$DisplayName
    )

    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        throw "winget is required to install $DisplayName automatically."
    }

    Invoke-Step "winget install --id $Id --exact --accept-package-agreements --accept-source-agreements --silent"
}

function Ensure-Docker {
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        return
    }

    if (-not $Yes.IsPresent) {
        $answer = Read-Host "Docker is missing. Install Docker Desktop now? [y/N]"
        if ($answer -notin @("y", "Y")) {
            throw "Docker is required for container bootstrap."
        }
    }

    if (-not (Test-Admin) -and -not $DryRun.IsPresent) {
        throw "Installing Docker Desktop requires elevated PowerShell (Run as Administrator)."
    }

    Install-WingetPackage -Id "Docker.DockerDesktop" -DisplayName "Docker Desktop"
}

function Ensure-DockerDaemon {
    if ($DryRun.IsPresent) {
        Write-Host "[dry-run] docker info"
        return
    }

    try {
        Invoke-Step "docker info > $null"
    }
    catch {
        throw "Docker daemon is not running. Start Docker Desktop and retry."
    }
}

function Run-OptionalDaemonSmoke {
    if ([string]::IsNullOrWhiteSpace($StartDaemonDuration)) {
        return
    }

    $resolvedWorkspace = $Workspace
    if ([string]::IsNullOrWhiteSpace($resolvedWorkspace)) {
        $resolvedWorkspace = "."
    }

    if ($DryRun.IsPresent) {
        Write-Host "[dry-run] docker run --rm -v `"$resolvedWorkspace:/work`" -w /work $Image background-agent daemon --duration $StartDaemonDuration"
        return
    }

    $abs = (Resolve-Path $resolvedWorkspace).Path
    Invoke-Step "docker run --rm -v `"$abs:/work`" -w /work $Image background-agent daemon --duration $StartDaemonDuration"
}

function Run-ContainerSmoke {
    Invoke-Step "docker pull $Image"
    Invoke-Step "docker run --rm $Image --help"

    if ($WithSdk.IsPresent) {
        Invoke-Step "docker pull $SdkImage"
        Invoke-Step "docker run --rm $SdkImage dotnet --info"
    }

    if (-not [string]::IsNullOrWhiteSpace($Workspace)) {
        $resolvedWorkspace = $Workspace
        if (-not $DryRun.IsPresent) {
            $resolvedWorkspace = (Resolve-Path $Workspace).Path
        }
        Invoke-Step "docker run --rm -v \"$resolvedWorkspace:/work\" -w /work $Image --help"
        if ($WithSdk.IsPresent) {
            Invoke-Step "docker run --rm -v \"$resolvedWorkspace:/work\" -w /work $SdkImage dotnet --info"
            if ($DryRun.IsPresent) {
                Write-Host "[dry-run] docker run --rm -v `"$resolvedWorkspace:/work`" -w /work $SdkImage bash -lc 'if [ -f application/src/Nexo.CLI/Nexo.CLI.csproj ]; then dotnet restore application/src/Nexo.CLI/Nexo.CLI.csproj; else echo no_cli_project_found; fi'"
            }
            else {
                Invoke-Step "docker run --rm -v \"$resolvedWorkspace:/work\" -w /work $SdkImage sh -lc 'if [ -f application/src/Nexo.CLI/Nexo.CLI.csproj ]; then dotnet restore application/src/Nexo.CLI/Nexo.CLI.csproj; else echo no_cli_project_found; fi'"
            }
        }
    }
}

Ensure-Windows

if ($Guided.IsPresent) {
    Write-Host "============================================="
    Write-Host " Nexo Guided Container Bootstrap (Windows)"
    Write-Host "============================================="
    Write-Host ""
    Write-Host "This setup will:"
    Write-Host "  1) ensure Docker Desktop is installed"
    Write-Host "  2) verify Docker is running"
    Write-Host "  3) pull Nexo CLI and SDK images"
    Write-Host "  4) run smoke checks"
    Write-Host ""
    Write-Host "You do NOT need to know containers to continue."
    Write-Host ""
}

Ensure-Docker
Ensure-DockerDaemon
Run-ContainerSmoke
Run-OptionalDaemonSmoke

Write-Host ""
Write-Host "Container bootstrap complete."
Write-Host "Image: $Image"
if ($WithSdk.IsPresent) {
    Write-Host "SDK image: $SdkImage"
}
Write-Host ""
Write-Host "Examples:"
Write-Host "  docker run --rm $Image --help"
if (-not [string]::IsNullOrWhiteSpace($Workspace)) {
    Write-Host "  docker run --rm -v \"$Workspace:/work\" -w /work $Image pipeline validate --template /work/path/to/template.json"
    if ($WithSdk.IsPresent) {
        Write-Host "  docker run --rm -v \"$Workspace:/work\" -w /work $SdkImage dotnet restore application/src/Nexo.CLI/Nexo.CLI.csproj"
    }
}

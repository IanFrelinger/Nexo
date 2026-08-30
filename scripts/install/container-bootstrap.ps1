[CmdletBinding()]
param(
    [string]$Image = "ghcr.io/ianfrelinger/nexo-cli:latest",
    [string]$SdkImage = "mcr.microsoft.com/dotnet/sdk:10.0",
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
                Write-Host "[dry-run] docker run --rm -v `"$resolvedWorkspace:/work`" -w /work $SdkImage bash -lc 'if [ -f application/src/Ashlar.CLI/Ashlar.CLI.csproj ]; then dotnet restore application/src/Ashlar.CLI/Ashlar.CLI.csproj; else echo no_cli_project_found; fi'"
            }
            else {
                Invoke-Step "docker run --rm -v \"$resolvedWorkspace:/work\" -w /work $SdkImage sh -lc 'if [ -f application/src/Ashlar.CLI/Ashlar.CLI.csproj ]; then dotnet restore application/src/Ashlar.CLI/Ashlar.CLI.csproj; else echo no_cli_project_found; fi'"
            }
        }
    }
}

# Install the `ashlar` host wrapper: docker exec into the running node, so the box has ONE
# operator identity (a host build would mint a second one under ~/.ashlar). Phase 1 step 8.
function Install-HostWrapper {
    $src = Join-Path $PSScriptRoot "ashlar-wrapper.ps1"
    if (-not (Test-Path $src)) {
        Write-Host "ashlar-wrapper.ps1 not found next to this script; skipping host command install"
        return
    }
    if ($DryRun.IsPresent) {
        Write-Host "[dry-run] would install the ashlar host command"
        return
    }
    $binDir = Join-Path $env:LOCALAPPDATA "Ashlar\bin"
    New-Item -ItemType Directory -Force -Path $binDir | Out-Null
    Copy-Item $src (Join-Path $binDir "ashlar.ps1") -Force
    Set-Content -Path (Join-Path $binDir "ashlar.cmd") -Encoding Ascii -Value "@powershell -NoProfile -ExecutionPolicy Bypass -File `"%~dp0ashlar.ps1`" %*"
    Write-Host "Installed host command: $binDir\ashlar.cmd"
    if ((($env:Path -split ';') | Where-Object { $_ -eq $binDir }).Count -eq 0) {
        Write-Host "  PATH does not include it yet; add once with:  setx PATH `"%PATH%;$binDir`""
    }
}

Ensure-Windows

if ($Guided.IsPresent) {
    Write-Host "============================================="
    Write-Host " Ashlar Guided Container Bootstrap (Windows)"
    Write-Host "============================================="
    Write-Host ""
    Write-Host "This setup will:"
    Write-Host "  1) ensure Docker Desktop is installed"
    Write-Host "  2) verify Docker is running"
    Write-Host "  3) pull Ashlar CLI and SDK images"
    Write-Host "  4) run smoke checks"
    Write-Host ""
    Write-Host "You do NOT need to know containers to continue."
    Write-Host ""
}

Ensure-Docker
Ensure-DockerDaemon
Run-ContainerSmoke
Run-OptionalDaemonSmoke
Install-HostWrapper

Write-Host ""
Write-Host "Container bootstrap complete."
Write-Host "Image: $Image"
if ($WithSdk.IsPresent) {
    Write-Host "SDK image: $SdkImage"
}
Write-Host ""
Write-Host "THE NODE - durable: identity, packages and trust history survive docker rm:"
Write-Host "  docker compose -f deploy/node.yml up -d      # from this checkout's root"
Write-Host "  docker ps                                    # heartbeat-backed health: a parked node shows unhealthy"
Write-Host "  ashlar keys show                             # this box's operator identity, via the installed wrapper"
Write-Host ""
Write-Host "One-off CLI (stateless):"
Write-Host "  docker run --rm $Image --help"
if (-not [string]::IsNullOrWhiteSpace($Workspace)) {
    Write-Host "  docker run --rm -v \"$Workspace:/work\" -w /work $Image pipeline validate --template /work/path/to/template.json"
    if ($WithSdk.IsPresent) {
        Write-Host "  docker run --rm -v \"$Workspace:/work\" -w /work $SdkImage dotnet restore application/src/Ashlar.CLI/Ashlar.CLI.csproj"
    }
}

[CmdletBinding()]
param(
    [string]$Image = "ghcr.io/ianfrelinger/nexo-cli:latest",
    [string]$SdkImage = "mcr.microsoft.com/dotnet/sdk:9.0",
    [string]$Workspace = ".",
    [switch]$Yes,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$entryScript = Join-Path $scriptDir "container-bootstrap.ps1"

if (-not (Test-Path $entryScript)) {
    throw "Missing script: $entryScript"
}

Write-Host "============================================="
Write-Host " Nexo Container One-Click Setup"
Write-Host "============================================="
Write-Host ""
Write-Host "This setup will:"
Write-Host "  1) ensure Docker Desktop is installed"
Write-Host "  2) verify Docker is running"
Write-Host "  3) pull Nexo CLI and SDK images"
Write-Host "  4) run smoke checks in containers"
Write-Host ""
Write-Host "You do NOT need to know Docker internals."
Write-Host ""

$invokeArgs = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $entryScript,
    "-Image", $Image,
    "-SdkImage", $SdkImage,
    "-Workspace", $Workspace
)

if ($Yes) { $invokeArgs += "-Yes" }
if ($DryRun) { $invokeArgs += "-DryRun" }

& powershell @invokeArgs
exit $LASTEXITCODE

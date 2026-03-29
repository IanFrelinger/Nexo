[CmdletBinding()]
param(
    [string]$InstallDir = "$HOME\Nexo",
    [switch]$Hero,
    [switch]$StartDaemon,
    [string]$DaemonDuration = "30s"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$installer = Join-Path $scriptDir "install.ps1"

if (-not (Test-Path $installer)) {
    throw "Missing installer script: $installer"
}

$args = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $installer,
    "-InstallDir", $InstallDir,
    "-Yes"
)

if ($Hero.IsPresent) {
    $args += "-Hero"
}
if ($StartDaemon.IsPresent) {
    $args += "-StartDaemon"
    $args += @("-DaemonDuration", $DaemonDuration)
}

Write-Host "Launching Nexo one-click installer..."
& powershell @args
exit $LASTEXITCODE

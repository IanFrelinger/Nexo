[CmdletBinding()]
param(
    [switch]$NoPause
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
$installer = Join-Path $repoRoot "scripts\install\install.ps1"

Write-Host "============================================="
Write-Host " Nexo Windows Zero-to-Hero Installer"
Write-Host "============================================="
Write-Host ""
Write-Host "This runs install, CLI verification, doctor,"
Write-Host "and a first quickstart pipeline run."
Write-Host ""

if (-not (Test-Path $installer)) {
    throw "Missing installer script: $installer"
}

$installDir = Join-Path $HOME "Nexo"
$exitCode = 0

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $installer -Yes -Hero
    if ($LASTEXITCODE -ne 0) {
        throw "Installer exited with code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "Done. If you want to run commands manually next:"
    Write-Host "  Set-Location `"$installDir`""
    Write-Host "  dotnet run --project src\Nexo.CLI -- --help"
}
catch {
    $exitCode = 1
    Write-Error $_
}

if (-not $NoPause.IsPresent) {
    Write-Host ""
    Read-Host "Press Enter to close this window"
}

exit $exitCode

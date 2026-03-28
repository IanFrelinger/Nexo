[CmdletBinding()]
param(
    [string]$Image = "ghcr.io/ianfrelinger/nexo-cli:latest",
    [string]$Workspace,
    [switch]$Yes,
    [switch]$DryRun
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$entryScript = Join-Path $scriptDir "container-bootstrap.ps1"

if (-not (Test-Path $entryScript)) {
    throw "Missing script: $entryScript"
}

$invokeArgs = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $entryScript,
    "-Image", $Image
)

if (-not [string]::IsNullOrWhiteSpace($Workspace)) { $invokeArgs += @("-Workspace", $Workspace) }
if ($Yes) { $invokeArgs += "-Yes" }
if ($DryRun) { $invokeArgs += "-DryRun" }

& powershell @invokeArgs
exit $LASTEXITCODE

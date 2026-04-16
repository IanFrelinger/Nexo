[CmdletBinding()]
param(
    [ValidateSet("check", "apply", "restore", "all")]
    [string]$Mode = "check",
    [switch]$IncludeOptional,
    [switch]$SkipRuntimeStudioTune
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
$windowsSetup = Join-Path $scriptDir "setup-windows.ps1"

if (-not (Test-Path $windowsSetup)) {
    throw "Missing setup script: $windowsSetup"
}

$args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $windowsSetup, "-Mode", $Mode)
if ($IncludeOptional) { $args += "-IncludeOptional" }
if ($SkipRuntimeStudioTune) { $args += "-SkipRuntimeStudioTune" }

Push-Location $repoRoot
try {
    & powershell @args
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}

[CmdletBinding()]
param(
    [string]$RepoUrl = "https://github.com/IanFrelinger/Nexo.git",
    [string]$InstallDir = "$HOME\Nexo",
    [string]$Branch,
    [switch]$IncludeOptional,
    [switch]$Yes,
    [switch]$SkipBuild,
    [switch]$RunContainerSmoke,
    [switch]$DryRun
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$entryScript = Join-Path $scriptDir "install.ps1"

if (-not (Test-Path $entryScript)) {
    throw "Missing installer script: $entryScript"
}

$invokeArgs = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $entryScript,
    "-RepoUrl", $RepoUrl,
    "-InstallDir", $InstallDir
)

if (-not [string]::IsNullOrWhiteSpace($Branch)) { $invokeArgs += @("-Branch", $Branch) }
if ($IncludeOptional) { $invokeArgs += "-IncludeOptional" }
if ($Yes) { $invokeArgs += "-Yes" }
if ($SkipBuild) { $invokeArgs += "-SkipBuild" }
if ($RunContainerSmoke) { $invokeArgs += "-RunContainerSmoke" }
if ($DryRun) { $invokeArgs += "-DryRun" }

& powershell @invokeArgs
exit $LASTEXITCODE

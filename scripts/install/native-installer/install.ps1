[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$installBase = if ([string]::IsNullOrWhiteSpace($env:NEXO_INSTALL_BASE)) {
    Join-Path $HOME ".local\bin"
}
else {
    $env:NEXO_INSTALL_BASE
}
$appSourceDir = Join-Path $scriptDir "bin\nexo-app"
$appTargetDir = Join-Path $installBase "nexo-app"
$targetPath = Join-Path $installBase "nexo.cmd"
$targetExe = Join-Path $appTargetDir "Nexo.CLI.exe"

if (-not (Test-Path $appSourceDir)) {
    throw "Missing bundled app directory at $appSourceDir"
}

New-Item -ItemType Directory -Path $installBase -Force | Out-Null
if (Test-Path $appTargetDir) {
    Remove-Item -Path $appTargetDir -Recurse -Force
}
Copy-Item $appSourceDir $appTargetDir -Recurse -Force
"@echo off`r`n`"$targetExe`" %*" | Set-Content -Path $targetPath -NoNewline

Write-Host "Installed nexo launcher to $targetPath"

$pathParts = @()
if (-not [string]::IsNullOrWhiteSpace($env:PATH)) {
    $pathParts = $env:PATH.Split(';')
}

if (-not ($pathParts -contains $installBase)) {
    Write-Host ""
    Write-Host "PATH update recommended: add $installBase"
    Write-Host "Example (current user):"
    Write-Host "  [Environment]::SetEnvironmentVariable('Path', `$env:Path + ';$installBase', 'User')"
}

Write-Host ""
Write-Host "Verify with:"
Write-Host "  nexo --help"

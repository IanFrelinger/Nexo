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
$targetPath = Join-Path $installBase "nexo.exe"
$sourcePath = Join-Path $scriptDir "bin\nexo.exe"

if (-not (Test-Path $sourcePath)) {
    throw "Missing bundled binary at $sourcePath"
}

New-Item -ItemType Directory -Path $installBase -Force | Out-Null
Copy-Item $sourcePath $targetPath -Force

Write-Host "Installed nexo to $targetPath"

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
Write-Host "  $targetPath --help"

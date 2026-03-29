[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$installBase = if ([string]::IsNullOrWhiteSpace($env:NEXO_INSTALL_BASE)) {
    Join-Path $HOME ".local\share\nexo"
}
else {
    $env:NEXO_INSTALL_BASE
}
$binDir = Join-Path $HOME ".local\bin"
$appSourceDir = Join-Path $scriptDir "app"
$appTargetDir = Join-Path $installBase "app"
$targetCmdPath = Join-Path $binDir "nexo.cmd"
$targetPs1Path = Join-Path $binDir "nexo.ps1"
$targetExe = Join-Path $appTargetDir "Nexo.CLI.exe"
$pathUpdated = $false

if (-not (Test-Path $appSourceDir)) {
    throw "Missing bundled app directory at $appSourceDir"
}

New-Item -ItemType Directory -Path $installBase -Force | Out-Null
New-Item -ItemType Directory -Path $binDir -Force | Out-Null
if (Test-Path $appTargetDir) {
    Remove-Item -Path $appTargetDir -Recurse -Force
}
Copy-Item $appSourceDir $appTargetDir -Recurse -Force
$cmdContent = "@echo off`r`n`"$targetExe`" %*"
$ps1Content = "& `"$targetExe`" @args"
$cmdContent | Set-Content -Path $targetCmdPath -NoNewline
$ps1Content | Set-Content -Path $targetPs1Path -NoNewline

Write-Host "Installed nexo app to $appTargetDir"
Write-Host "Installed launchers:"
Write-Host "  $targetCmdPath"
Write-Host "  $targetPs1Path"

$pathParts = @()
if (-not [string]::IsNullOrWhiteSpace($env:PATH)) {
    $pathParts = $env:PATH.Split(';')
}

if (-not ($pathParts -contains $binDir)) {
    Write-Host ""
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    if ([string]::IsNullOrWhiteSpace($userPath)) {
        [Environment]::SetEnvironmentVariable('Path', $binDir, 'User')
    }
    elseif ($userPath.Split(';') -notcontains $binDir) {
        [Environment]::SetEnvironmentVariable('Path', "$userPath;$binDir", 'User')
    }

    $env:PATH = "$binDir;$env:PATH"
    $pathUpdated = $true
    Write-Host "Added $binDir to the user PATH."
}

Write-Host ""
Write-Host "Verify with:"
Write-Host "  nexo --help"

Write-Host ""
Write-Host "Running post-install smoke check..."
& $targetExe --help | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Post-install validation failed. Command exited with code $LASTEXITCODE"
}
Write-Host "Post-install smoke check passed."

if ($pathUpdated) {
    Write-Host ""
    Write-Host "Open a new terminal to use 'nexo' from PATH everywhere."
}

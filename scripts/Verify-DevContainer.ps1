#!/usr/bin/env pwsh
<#
.SYNOPSIS
  One-shot verification of the Dev Container path: post-create restore + Nexo.CLI build + --help.

.DESCRIPTION
  Same checks as opening the repo in a Dev Container, without VS Code/Cursor.
  Requires Docker only (no Git Bash on Windows).

  From repository root:
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Verify-DevContainer.ps1

  CI and docs refer to this script as the canonical headless dev-container smoke.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string]$Start)
    $dir = if ([string]::IsNullOrWhiteSpace($Start)) { (Get-Location).Path } else { $Start }
    $d = [System.IO.DirectoryInfo]::new((Resolve-Path $dir).Path)
    while ($null -ne $d) {
        if (Test-Path (Join-Path $d.FullName "Nexo.sln")) {
            return $d.FullName.TrimEnd('\')
        }
        $d = $d.Parent
    }
    throw "Could not find Nexo.sln from: $Start"
}

try {
    $null = Get-Command docker -ErrorAction Stop
}
catch {
    throw "Docker is required. Install Docker Desktop or Docker Engine, then retry."
}

$root = Resolve-RepoRoot $RepoRoot
$mountSrc = $root.Replace('\', '/')
$image = "mcr.microsoft.com/devcontainers/dotnet:8.0-bookworm"

Write-Host "Verify-DevContainer: pulling $image ..." -ForegroundColor Cyan
& docker pull $image | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "docker pull failed (exit $LASTEXITCODE)"
}

# Git for Windows rewrites /workspace; Linux accepts /workspace.
$workDir = if ($IsWindows) { "//workspace" } else { "/workspace" }

$bashPayload = @'
set -euo pipefail
export DOTNET_ROLL_FORWARD=LatestMajor
bash .devcontainer/post-create.sh
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore -v minimal
dotnet run --project application/src/Nexo.CLI -- --help >/dev/null
echo "Verify-DevContainer: ok"
'@
$bashPayload = ($bashPayload -replace "`r`n", "`n") -replace "`r", ""
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($bashPayload))
$linuxCmd = "echo $b64 | base64 -d | bash"

Write-Host "Verify-DevContainer: post-create + build + CLI smoke (in container) ..." -ForegroundColor Cyan
& docker run --rm `
    -v "${mountSrc}:/workspace:rw" `
    -w $workDir `
    -e DOTNET_ROLL_FORWARD=LatestMajor `
    $image `
    bash -lc $linuxCmd

if ($LASTEXITCODE -ne 0) {
    throw "docker run failed (exit $LASTEXITCODE)"
}

Write-Host "Verify-DevContainer: success." -ForegroundColor Green

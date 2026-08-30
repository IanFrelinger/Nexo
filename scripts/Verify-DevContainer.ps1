#!/usr/bin/env pwsh
<#
.SYNOPSIS
  One-shot verification of the Dev Container path: post-create restore + Ashlar.CLI build + --help.

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
        if (Test-Path (Join-Path $d.FullName "Ashlar.sln")) {
            return $d.FullName.TrimEnd('\')
        }
        $d = $d.Parent
    }
    throw "Could not find Ashlar.sln from: $Start"
}

try {
    $null = Get-Command docker -ErrorAction Stop
}
catch {
    throw "Docker is required. Install Docker Desktop or Docker Engine, then retry."
}

$root = Resolve-RepoRoot $RepoRoot
$mountSrc = $root.Replace('\', '/')
$image = "mcr.microsoft.com/devcontainers/dotnet:10.0-noble"

Write-Host "Verify-DevContainer: pulling $image ..." -ForegroundColor Cyan
& docker pull $image | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "docker pull failed (exit $LASTEXITCODE)"
}

# Git for Windows rewrites /workspace; Linux accepts /workspace.
$workDir = if ($IsWindows) { "//workspace" } else { "/workspace" }

# DOTNET_ROLL_FORWARD is deliberately NOT exported here, and the stock devcontainer image
# is deliberately kept: this script's job is to verify the dev container as a developer
# actually gets it. post-create.sh installs the real ASP.NET Core 8 runtime, so asserting
# it is present afterwards is what proves that install still works — rolling forward would
# mask its absence, which is exactly how net8.0 HTTP-hosting tests started failing.
$bashPayload = @'
set -euo pipefail
bash .devcontainer/post-create.sh
dotnet --list-runtimes | grep -q '^Microsoft\.AspNetCore\.App 8\.' || {
  echo "Verify-DevContainer: post-create did not leave an ASP.NET Core 8 runtime installed;" >&2
  echo "  net8.0 targets would roll forward onto ASP.NET Core 10 and HTTP-hosting tests would fail." >&2
  exit 1
}
dotnet build application/src/Ashlar.CLI/Ashlar.CLI.csproj --no-restore -v minimal
dotnet run --project application/src/Ashlar.CLI -- --help >/dev/null
echo "Verify-DevContainer: ok"
'@
$bashPayload = ($bashPayload -replace "`r`n", "`n") -replace "`r", ""
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($bashPayload))
$linuxCmd = "echo $b64 | base64 -d | bash"

Write-Host "Verify-DevContainer: post-create + build + CLI smoke (in container) ..." -ForegroundColor Cyan
& docker run --rm `
    -v "${mountSrc}:/workspace:rw" `
    -w $workDir `
    $image `
    bash -lc $linuxCmd

if ($LASTEXITCODE -ne 0) {
    throw "docker run failed (exit $LASTEXITCODE)"
}

Write-Host "Verify-DevContainer: success." -ForegroundColor Green

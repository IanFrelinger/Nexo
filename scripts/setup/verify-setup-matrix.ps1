#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Brute-force style verification of Ashlar setup paths (native + Docker + compose).

.DESCRIPTION
  Runs many independent scenarios (fresh cwd, NuGet cache isolation, repeat restores,
  compose config validation, optional Docker image builds). Tier A failures fail the script;
  Docker-dependent cases skip with a clear reason when Docker is unavailable.

  In GitHub Actions (GITHUB_ACTIONS=true), Docker image builds are skipped unless
  ASHLAR_MATRIX_DOCKER_BUILD=1 is set (saves minutes; compose `config` still runs when Docker exists).
#>
[CmdletBinding()]
param(
    [string] $RepoRoot = "",
    [switch] $SkipDocker,
    [switch] $SkipDockerBuild,
    [switch] $IncludeOptionalDependencyCheck,
    [switch] $CiStrict
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string] $Start)
    $dir = if ([string]::IsNullOrWhiteSpace($Start)) { (Get-Location).Path } else { $Start }
    $d = [System.IO.DirectoryInfo]::new((Resolve-Path $dir).Path)
    while ($null -ne $d) {
        if (Test-Path (Join-Path $d.FullName "Ashlar.sln")) { return $d.FullName }
        $d = $d.Parent
    }
    throw "Could not find Ashlar.sln from: $Start"
}

$repo = Resolve-RepoRoot $RepoRoot
$setupPs1 = Join-Path $repo "scripts/setup/setup.ps1"
$setupWin = Join-Path $repo "scripts/setup/setup-windows.ps1"
$dockerRestore = Join-Path $repo "scripts/docker-restore.ps1"

$skipDockerBuildEffective = $SkipDockerBuild.IsPresent
if ($env:GITHUB_ACTIONS -eq "true" -and -not $env:ASHLAR_MATRIX_DOCKER_BUILD) {
    $skipDockerBuildEffective = $true
}

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [string] $Tier,
        [string] $Name,
        [bool] $Ok,
        [string] $Detail = ""
    )
    $script:results.Add([pscustomobject]@{ Tier = $Tier; Name = $Name; Ok = $Ok; Detail = $Detail })
}

function Invoke-Case {
    param(
        [string] $Tier,
        [string] $Name,
        [scriptblock] $Action
    )
    Write-Host "`n=== $Tier :: $Name ===" -ForegroundColor Cyan
    try {
        & $Action
        Add-Result -Tier $Tier -Name $Name -Ok $true
        Write-Host "OK  $Name" -ForegroundColor Green
    }
    catch {
        Add-Result -Tier $Tier -Name $Name -Ok $false -Detail $_.Exception.Message
        Write-Host "FAIL $Name :: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Test-DockerCli {
    try {
        $null = Get-Command docker -ErrorAction Stop
        & docker info 2>$null | Out-Null
        return ($LASTEXITCODE -eq 0)
    }
    catch { return $false }
}

# --- Tier A: must pass on a healthy dev machine ---
Invoke-Case -Tier "A" -Name "setup.ps1 -Mode check (from repo root)" -Action {
    Push-Location $repo
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $setupPs1 -Mode check
        if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
    }
    finally { Pop-Location }
}

Invoke-Case -Tier "A" -Name "setup.ps1 -Mode check (from TEMP cwd, absolute script path)" -Action {
    Push-Location $env:TEMP
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $setupPs1 -Mode check
        if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
    }
    finally { Pop-Location }
}

Invoke-Case -Tier "A" -Name "setup-windows.ps1 -Mode check (direct)" -Action {
    Push-Location $repo
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $setupWin -Mode check
        if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
    }
    finally { Pop-Location }
}

Invoke-Case -Tier "A" -Name "setup.ps1 -Mode restore" -Action {
    Push-Location $repo
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $setupPs1 -Mode restore
        if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
    }
    finally { Pop-Location }
}

Invoke-Case -Tier "A" -Name "setup.ps1 -Mode restore (repeat / idempotent)" -Action {
    Push-Location $repo
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $setupPs1 -Mode restore
        if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
    }
    finally { Pop-Location }
}

Invoke-Case -Tier "A" -Name "dotnet build Ashlar.CLI --no-restore (default NuGet cache)" -Action {
    Push-Location $repo
    try {
        & dotnet build "application/src/Ashlar.CLI/Ashlar.CLI.csproj" --no-restore -v minimal
        if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
    }
    finally { Pop-Location }
}

$isolatedNuget = Join-Path $env:TEMP ("ashlar-matrix-nuget-" + [Guid]::NewGuid().ToString("n"))
Invoke-Case -Tier "A" -Name "restore + build with isolated NUGET_PACKAGES" -Action {
    Push-Location $repo
    try {
        New-Item -ItemType Directory -Force -Path $isolatedNuget | Out-Null
        $prev = $env:NUGET_PACKAGES
        $env:NUGET_PACKAGES = $isolatedNuget
        try {
            & powershell -NoProfile -ExecutionPolicy Bypass -File $setupPs1 -Mode restore
            if ($LASTEXITCODE -ne 0) { throw "setup restore exit $LASTEXITCODE" }
            & dotnet build "application/src/Ashlar.CLI/Ashlar.CLI.csproj" --no-restore -v minimal
            if ($LASTEXITCODE -ne 0) { throw "build exit $LASTEXITCODE" }
        }
        finally {
            if ($null -ne $prev) { $env:NUGET_PACKAGES = $prev } else { Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue }
        }
    }
    finally { Pop-Location }
}

if ($IncludeOptionalDependencyCheck.IsPresent) {
    Invoke-Case -Tier "B" -Name "setup.ps1 -Mode check -IncludeOptional (requires docker + ollama on PATH)" -Action {
        Push-Location $repo
        try {
            & powershell -NoProfile -ExecutionPolicy Bypass -File $setupPs1 -Mode check -IncludeOptional
            if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
        }
        finally { Pop-Location }
    }
}
else {
    Add-Result -Tier "B" -Name "setup.ps1 -Mode check -IncludeOptional (skipped; pass -IncludeOptionalDependencyCheck)" -Ok $true -Detail "skipped"
}

# --- Tier B: Docker / compose (best-effort; skip if no engine) ---
if ($SkipDocker.IsPresent) {
    Add-Result -Tier "B" -Name "Docker tier" -Ok $true -Detail "skipped (-SkipDocker)"
}
elseif (-not (Test-DockerCli)) {
    Add-Result -Tier "B" -Name "Docker tier" -Ok $true -Detail "skipped (docker not available)"
}
else {
    $composeFiles = @(
        "deploy/compose/docker-compose.agent-server.yml",
        "deploy/compose/docker-compose.portal.yml",
        "deploy/compose/docker-compose.test.yml",
        "deploy/compose/docker-compose.ephemeral.yml",
        "deploy/compose/docker-compose.ollama.yml"
    )
    foreach ($cf in $composeFiles) {
        $p = Join-Path $repo $cf
        if (-not (Test-Path $p)) { continue }
        $proj = "mtx-" + ($cf -replace "[^a-zA-Z0-9]", "")
        Invoke-Case -Tier "B" -Name "docker compose config: $cf" -Action {
            Push-Location $repo
            try {
                & docker compose -p $proj -f $cf config 2>&1 | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "docker compose config exit $LASTEXITCODE" }
            }
            finally { Pop-Location }
        }
    }

    Invoke-Case -Tier "B" -Name "docker-restore.ps1 (restore graph in Linux SDK container)" -Action {
        if (-not (Test-Path $dockerRestore)) { throw "missing $dockerRestore" }
        & powershell -NoProfile -ExecutionPolicy Bypass -File $dockerRestore -RepoRoot $repo
        if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
    }

    if (-not $skipDockerBuildEffective) {
        Invoke-Case -Tier "B" -Name "docker compose build (agent-server ashlar-api)" -Action {
            Push-Location $repo
            try {
                & docker compose -f "deploy/compose/docker-compose.agent-server.yml" build ashlar-api
                if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
            }
            finally { Pop-Location }
        }
        Invoke-Case -Tier "B" -Name "docker compose build (portal ashlar-api)" -Action {
            Push-Location $repo
            try {
                & docker compose -p "mtx-portal" -f "deploy/compose/docker-compose.portal.yml" build ashlar-api
                if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
            }
            finally { Pop-Location }
        }
    }
    else {
        Add-Result -Tier "B" -Name "docker compose build (skipped)" -Ok $true -Detail "CI default or -SkipDockerBuild"
    }

    if (-not $skipDockerBuildEffective) {
        Invoke-Case -Tier "B" -Name "docker-restore.ps1 -Build" -Action {
            & powershell -NoProfile -ExecutionPolicy Bypass -File $dockerRestore -RepoRoot $repo -Build
            if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
        }
    }
}

# --- Tier C: in-container Linux setup (requires Docker) ---
if (-not $SkipDocker.IsPresent -and (Test-DockerCli)) {
    $images = @(
        @{ Tag = "mcr.microsoft.com/dotnet/sdk:10.0"; Id = "sdk-10-default" },
        @{ Tag = "mcr.microsoft.com/dotnet/sdk:10.0-noble"; Id = "sdk-10-noble" }
    )
    foreach ($img in $images) {
        Invoke-Case -Tier "C" -Name "Linux container: $($img.Id) setup-linux check+restore+CLI build" -Action {
            $bash = @'
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive
if command -v apt-get >/dev/null 2>&1; then
  apt-get update
  apt-get install -y ca-certificates curl git gnupg bash
fi
bash scripts/setup/setup-linux.sh check
bash scripts/setup/setup-linux.sh restore
dotnet build application/src/Ashlar.CLI/Ashlar.CLI.csproj --no-restore -v minimal
'@
            & docker pull $img.Tag 2>&1 | Out-Host
            & docker run --rm -v "${repo}:/repo" -w /repo $img.Tag bash -lc $bash
            if ($LASTEXITCODE -ne 0) { throw "container exit $LASTEXITCODE" }
        }
    }
}

elseif (-not $SkipDocker.IsPresent) {
    Add-Result -Tier "C" -Name "Linux container matrix" -Ok $true -Detail "skipped (docker not available)"
}

# --- Report ---
$failed = @($results | Where-Object { -not $_.Ok })
Write-Host "`n========== SETUP MATRIX SUMMARY ==========" -ForegroundColor Cyan
$results | Format-Table -AutoSize
$reportPath = Join-Path $repo (Join-Path ".ashlar" "setup-matrix-report.json")
New-Item -ItemType Directory -Force -Path (Split-Path $reportPath) | Out-Null
$results | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 $reportPath
Write-Host "Wrote $reportPath"

$aFail = @($results | Where-Object { $_.Tier -eq "A" -and -not $_.Ok }).Count
if ($failed.Count -gt 0) {
    Write-Host "`nFAILED cases (all tiers): $($failed.Count)" -ForegroundColor Red
    if ($aFail -gt 0) {
        Write-Host "Tier A (native setup) failures: $aFail - core setup is NOT seamless." -ForegroundColor Red
        exit 1
    }
    Write-Host "Tier A passed; failures were in optional Docker/container lanes only." -ForegroundColor Yellow
    if ($CiStrict.IsPresent) { exit 2 }
    exit 0
}

if ($CiStrict.IsPresent -and $skipDockerBuildEffective -and -not $SkipDocker.IsPresent -and (Test-DockerCli)) {
    Write-Warning "CiStrict: Docker was available but image builds were skipped (CI default). Set ASHLAR_MATRIX_DOCKER_BUILD=1 for full Docker build coverage in CI."
}

Write-Host "`nAll executed matrix cases passed." -ForegroundColor Green
exit 0

#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet("check", "restore", "all", "apply")]
    [string]$Mode = "check",
    [switch]$IncludeOptional,
    [switch]$Yes,
    [switch]$Tune,
    [switch]$SkipRuntimeStudioTune
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..")).Path

function Test-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Ensure-Windows {
    if (-not $env:OS -or $env:OS -ne "Windows_NT") {
        throw "This script only supports Windows hosts."
    }
}

function Test-CommandExists {
    param([Parameter(Mandatory = $true)][string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-DotnetMajor {
    if (-not (Test-CommandExists -Name "dotnet")) { return 0 }
    $version = (& dotnet --version) 2>$null
    if ([string]::IsNullOrWhiteSpace($version)) { return 0 }
    return [int]($version.Split('.')[0])
}

function Test-SupportedDotnet {
    return (Get-DotnetMajor) -ge 10
}

function Install-WingetPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$DisplayName
    )
    if (-not (Test-CommandExists -Name "winget")) {
        throw "winget is required to install $DisplayName automatically."
    }

    & winget install --id $Id --exact --accept-package-agreements --accept-source-agreements --silent
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install $DisplayName via winget (exit $LASTEXITCODE)."
    }
}

function Ensure-Winget {
    if (-not (Test-CommandExists -Name "winget")) {
        throw "winget is required for auto-install mode. Install App Installer from Microsoft Store and retry."
    }
}

function Invoke-ApplyDependencies {
    $requiredToInstall = New-Object System.Collections.Generic.List[string]
    $optionalToInstall = New-Object System.Collections.Generic.List[string]

    if (-not (Test-CommandExists -Name "git")) { $requiredToInstall.Add("git") }
    if (-not (Test-CommandExists -Name "curl")) { $requiredToInstall.Add("curl") }
    if (-not (Test-SupportedDotnet)) { $requiredToInstall.Add("dotnet") }

    if (-not (Test-CommandExists -Name "docker")) { $optionalToInstall.Add("docker") }
    if (-not (Test-CommandExists -Name "ollama")) { $optionalToInstall.Add("ollama") }

    if ($requiredToInstall.Count -eq 0 -and ((-not $IncludeOptional.IsPresent) -or $optionalToInstall.Count -eq 0)) {
        Write-Host "No dependency installation required."
        return
    }

    Ensure-Winget

    foreach ($dep in $requiredToInstall) {
        switch ($dep) {
            "git" {
                Write-Host "Installing Git..."
                Install-WingetPackage -Id "Git.Git" -DisplayName "Git"
            }
            "curl" {
                Write-Host "curl is bundled with modern Windows builds. Skipping auto-install."
            }
            "dotnet" {
                Write-Host "Installing .NET SDK 10..."
                Install-WingetPackage -Id "Microsoft.DotNet.SDK.10" -DisplayName ".NET SDK 10"
            }
            default {
                throw "Unsupported required dependency in apply mode: $dep"
            }
        }
    }

    if ($IncludeOptional.IsPresent) {
        foreach ($dep in $optionalToInstall) {
            switch ($dep) {
                "docker" {
                    Write-Host "Installing Docker Desktop (optional)..."
                    Install-WingetPackage -Id "Docker.DockerDesktop" -DisplayName "Docker Desktop"
                }
                "ollama" {
                    Write-Host "Installing Ollama (optional)..."
                    Install-WingetPackage -Id "Ollama.Ollama" -DisplayName "Ollama"
                }
            }
        }
    }

    Invoke-DependencyCheck
}

function Ensure-RepoFiles {
    $required = @(
        (Join-Path $RepoRoot "Nexo.sln"),
        (Join-Path $RepoRoot "src\Nexo.Core.Application\Nexo.Core.Application.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Infrastructure\Nexo.Infrastructure.csproj"),
        (Join-Path $RepoRoot "application\src\Nexo.CLI\Nexo.CLI.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Tests.Infrastructure\scripts\copy-assemblies.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Tests.Infrastructure\Nexo.Tests.Infrastructure.csproj")
    )
    foreach ($file in $required) {
        if (-not (Test-Path $file)) {
            throw "Required repository file not found: $file"
        }
    }
}

function Invoke-Restore {
    Ensure-RepoFiles
    if (-not (Test-CommandExists -Name "dotnet")) {
        throw "dotnet not found. Install .NET SDK 10+ via your IDE, then re-run setup check/restore."
    }

    $restoreTargets = @(
        (Join-Path $RepoRoot "src\Nexo.Core.Application\Nexo.Core.Application.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Infrastructure\Nexo.Infrastructure.csproj"),
        (Join-Path $RepoRoot "application\src\Nexo.CLI\Nexo.CLI.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Tests.Infrastructure\scripts\copy-assemblies.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Tests.Infrastructure\Nexo.Tests.Infrastructure.csproj")
    )
    foreach ($target in $restoreTargets) {
        & dotnet restore $target
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed: $target" }
    }
}

function Invoke-DependencyCheck {
    $missingRequired = New-Object System.Collections.Generic.List[string]
    $missingOptional = New-Object System.Collections.Generic.List[string]

    Write-Host "Checking required dependencies (Windows)..."

    if (Test-CommandExists -Name "git") {
        Write-Host "  [OK] git"
    } else {
        Write-Host "  [MISSING] git"
        $missingRequired.Add("git")
    }

    if (Test-CommandExists -Name "curl") {
        Write-Host "  [OK] curl"
    } else {
        Write-Host "  [MISSING] curl"
        $missingRequired.Add("curl")
    }

    if (Test-SupportedDotnet) {
        Write-Host "  [OK] dotnet SDK >= 10"
    } else {
        Write-Host "  [MISSING] dotnet SDK >= 10"
        $missingRequired.Add("dotnet")
    }

    if (Test-CommandExists -Name "docker") {
        Write-Host "  [OK] docker (optional)"
    } else {
        Write-Host "  [MISSING] docker (optional)"
        $missingOptional.Add("docker")
    }

    if (Test-CommandExists -Name "ollama") {
        Write-Host "  [OK] ollama (optional)"
    } else {
        Write-Host "  [MISSING] ollama (optional)"
        $missingOptional.Add("ollama")
    }

    if ($missingRequired.Count -gt 0) {
        foreach ($dep in $missingRequired) {
            switch ($dep) {
                "git" { Write-Host "  - Install Git via your IDE or system installer." }
                "curl" { Write-Host "  - Install curl via system tooling." }
                "dotnet" { Write-Host "  - Install .NET SDK 10+ via your IDE installer (recommended)." }
                default { Write-Host "  - Install $dep manually." }
            }
        }
        throw "Missing required dependencies: $($missingRequired -join ', ')"
    }

    if ($IncludeOptional.IsPresent -and $missingOptional.Count -gt 0) {
        foreach ($dep in $missingOptional) {
            switch ($dep) {
                "docker" { Write-Host "  - Install Docker Desktop manually if needed." }
                "ollama" { Write-Host "  - Install Ollama manually if needed." }
                default { Write-Host "  - Install $dep manually." }
            }
        }
        throw "Missing optional dependencies (requested): $($missingOptional -join ', ')"
    }

    Write-Host "Dependency check passed."
}

# Opt-in (-Tune). The benchmark runs `nexo workflow optimize` against local Ollama models for
# several minutes (needs Git Bash); the tuned ModelName values land in the gitignored
# .nexo\runtime-studio\agent_set.local.json (seeded from the tracked
# apps\runtime-studio\config\agent_set.local.json), so `-Mode all` never edits a tracked file.
function Invoke-RuntimeStudioAutoTune {
    if (-not $Tune.IsPresent) {
        Write-Host "Runtime Studio hardware tune not requested (pass -Tune to run the optional multi-minute Ollama benchmark; requires Git Bash)."
        return
    }

    if ($SkipRuntimeStudioTune.IsPresent) {
        Write-Host "Skipping Runtime Studio hardware tune (-SkipRuntimeStudioTune)."
        return
    }

    if ($env:NEXO_SKIP_RUNTIME_STUDIO_TUNE -eq '1') {
        Write-Host "Skipping Runtime Studio hardware tune (NEXO_SKIP_RUNTIME_STUDIO_TUNE=1)."
        return
    }

    if ($env:CI -eq 'true' -or $env:GITHUB_ACTIONS -eq 'true') {
        Write-Host "Skipping Runtime Studio hardware tune (CI environment)."
        return
    }

    $tuneScript = Join-Path $RepoRoot "apps\runtime-studio\scripts\optimize_agent_cluster.sh"
    if (-not (Test-Path $tuneScript)) {
        return
    }

    if (-not (Test-CommandExists -Name "dotnet")) {
        Write-Warning "dotnet not available; skipping Runtime Studio hardware tune."
        return
    }

    $pf86 = ${env:ProgramFiles(x86)}
    $bashCandidates = @(
        (Join-Path $env:ProgramFiles "Git\bin\bash.exe"),
        (Join-Path $pf86 "Git\bin\bash.exe")
    )
    $bash = $null
    foreach ($candidate in $bashCandidates) {
        if (Test-Path $candidate) {
            $bash = $candidate
            break
        }
    }

    if (-not $bash -and (Test-CommandExists -Name "bash")) {
        $bash = "bash"
    }

    if (-not $bash) {
        Write-Warning @"
Git Bash not found; skipping Runtime Studio hardware tune.
Install Git for Windows, or run manually from repo root:
  bash apps/runtime-studio/scripts/optimize_agent_cluster.sh --skip-daemon --budget-runs 48
"@
        return
    }

    Write-Host ""
    Write-Host "Runtime Studio: benchmarking local models/compositions (bounded budget). This may take several minutes."
    Write-Host "Tuned agent set is written to .nexo\runtime-studio\agent_set.local.json (gitignored)."
    Write-Host ""

    Push-Location $RepoRoot
    try {
        if ($bash -eq "bash") {
            & bash "apps/runtime-studio/scripts/optimize_agent_cluster.sh" --skip-daemon --budget-runs 24
        }
        else {
            & $bash -c "bash 'apps/runtime-studio/scripts/optimize_agent_cluster.sh' --skip-daemon --budget-runs 24"
        }

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Runtime Studio auto-tune finished with exit code $LASTEXITCODE (dependencies optional; you can re-run the script later)."
        }
    }
    finally {
        Pop-Location
    }
}

function Disable-ApplyMode {
    throw "Disable-ApplyMode is deprecated and should not be called."
}

Ensure-Windows

switch ($Mode) {
    "check" {
        Invoke-DependencyCheck
    }
    "apply" {
        Invoke-ApplyDependencies
    }
    "restore" {
        Invoke-Restore
    }
    "all" {
        Invoke-ApplyDependencies
        Invoke-DependencyCheck
        Invoke-Restore
        Invoke-RuntimeStudioAutoTune
    }
    default {
        throw "Unknown mode: $Mode"
    }
}

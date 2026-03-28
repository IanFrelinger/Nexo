#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet("check", "restore", "all", "apply")]
    [string]$Mode = "check",
    [switch]$IncludeOptional,
    [switch]$Yes
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
    return (Get-DotnetMajor) -ge 9
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

function Ensure-RepoFiles {
    $required = @(
        (Join-Path $RepoRoot "Nexo.sln"),
        (Join-Path $RepoRoot "src\Nexo.Core.Application\Nexo.Core.Application.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Infrastructure\Nexo.Infrastructure.csproj"),
        (Join-Path $RepoRoot "src\Nexo.CLI\Nexo.CLI.csproj"),
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
        throw "dotnet not found. Install .NET SDK 9+ via your IDE, then re-run setup check/restore."
    }

    $restoreTargets = @(
        (Join-Path $RepoRoot "src\Nexo.Core.Application\Nexo.Core.Application.csproj"),
        (Join-Path $RepoRoot "src\Nexo.Infrastructure\Nexo.Infrastructure.csproj"),
        (Join-Path $RepoRoot "src\Nexo.CLI\Nexo.CLI.csproj"),
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
        Write-Host "  [OK] dotnet SDK >= 9"
    } else {
        Write-Host "  [MISSING] dotnet SDK >= 9"
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
                "dotnet" { Write-Host "  - Install .NET SDK 9+ via your IDE installer (recommended)." }
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

function Disable-ApplyMode {
    throw @"
Mode 'apply' has been removed.
This repository no longer auto-installs host dependencies from setup scripts.
Install prerequisites via your IDE/system installer, then run:
  .\scripts\setup\setup.ps1 -Mode check
"@
}

Ensure-Windows

switch ($Mode) {
    "check" {
        Invoke-DependencyCheck
    }
    "apply" {
        Disable-ApplyMode
    }
    "restore" {
        Invoke-Restore
    }
    "all" {
        Invoke-DependencyCheck
        Invoke-Restore
    }
    default {
        throw "Unknown mode: $Mode"
    }
}

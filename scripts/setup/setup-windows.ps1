#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet("check", "apply", "restore", "all")]
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
        throw "dotnet not found. Run apply first."
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
        throw "Missing required dependencies: $($missingRequired -join ', ')"
    }

    if ($IncludeOptional.IsPresent -and $missingOptional.Count -gt 0) {
        throw "Missing optional dependencies (requested): $($missingOptional -join ', ')"
    }

    Write-Host "Dependency check passed."
}

function Invoke-DependencyInstall {
    $missingRequired = New-Object System.Collections.Generic.List[string]
    $missingOptional = New-Object System.Collections.Generic.List[string]

    if (-not (Test-CommandExists -Name "git")) { $missingRequired.Add("git") }
    if (-not (Test-CommandExists -Name "curl")) { $missingRequired.Add("curl") }
    if (-not (Test-SupportedDotnet)) { $missingRequired.Add("dotnet") }

    if ($IncludeOptional.IsPresent) {
        if (-not (Test-CommandExists -Name "docker")) { $missingOptional.Add("docker") }
        if (-not (Test-CommandExists -Name "ollama")) { $missingOptional.Add("ollama") }
    }

    if ($missingRequired.Count -eq 0 -and $missingOptional.Count -eq 0) {
        Write-Host "No dependencies to install."
        return
    }

    Write-Host "Install plan:"
    foreach ($dep in $missingRequired) { Write-Host "  - required: $dep" }
    foreach ($dep in $missingOptional) { Write-Host "  - optional: $dep" }

    if (-not $Yes.IsPresent) {
        $answer = Read-Host "Proceed with installation? [y/N]"
        if ($answer -notin @("y", "Y")) {
            throw "Cancelled."
        }
    }

    if (-not (Test-Admin)) {
        throw "Installation requires elevated PowerShell (Run as Administrator)."
    }

    foreach ($dep in $missingRequired) {
        switch ($dep) {
            "git" { Install-WingetPackage -Id "Git.Git" -DisplayName "Git" }
            "curl" { Install-WingetPackage -Id "cURL.cURL" -DisplayName "curl" }
            "dotnet" { Install-WingetPackage -Id "Microsoft.DotNet.SDK.9" -DisplayName ".NET SDK 9" }
            default { throw "Unsupported required dependency: $dep" }
        }
    }

    foreach ($dep in $missingOptional) {
        switch ($dep) {
            "docker" { Install-WingetPackage -Id "Docker.DockerDesktop" -DisplayName "Docker Desktop" }
            "ollama" { Install-WingetPackage -Id "Ollama.Ollama" -DisplayName "Ollama" }
            default { Write-Warning "Unsupported optional dependency: $dep" }
        }
    }
}

Ensure-Windows

switch ($Mode) {
    "check" {
        Invoke-DependencyCheck
    }
    "apply" {
        Invoke-DependencyInstall
        Invoke-DependencyCheck
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

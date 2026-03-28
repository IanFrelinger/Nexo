[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Rid,
    [Parameter(Mandatory = $true)][string]$OutputDir,
    [string]$Version = "dev"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
$templateDir = Join-Path $scriptDir "native-installer"

if (-not (Test-Path $templateDir)) {
    throw "Missing template directory: $templateDir"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet is required to package installer bundles."
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$publishDir = Join-Path ([System.IO.Path]::GetTempPath()) ("nexo-publish-" + [Guid]::NewGuid().ToString("N"))
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ("nexo-bundle-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $workDir -Force | Out-Null

try {
    Write-Host "Publishing self-contained CLI for $Rid..."
    & dotnet publish (Join-Path $repoRoot "src\Nexo.CLI\Nexo.CLI.csproj") `
        -c Release `
        -r $Rid `
        --self-contained true `
        -p:IncludeTestProjectReferences=false `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    $sourceBinary = Join-Path $publishDir "Nexo.CLI.exe"
    if (-not (Test-Path $sourceBinary)) {
        throw "Expected publish output not found: $sourceBinary"
    }

    $bundleName = "nexo-native-installer-windows-$Rid"
    $bundleDir = Join-Path $workDir $bundleName
    $binDir = Join-Path $bundleDir "bin"
    New-Item -ItemType Directory -Path $binDir -Force | Out-Null

    Copy-Item $sourceBinary (Join-Path $binDir "nexo.exe")
    Copy-Item (Join-Path $templateDir "install.ps1") (Join-Path $bundleDir "install.ps1")

    $readmePath = Join-Path $bundleDir "README.md"
    @"
# Nexo Native Installer (windows / $Rid)

Version: $Version

This bundle contains a self-contained `nexo.exe` CLI binary and an install wrapper.

## Install

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1
```

## What install does

1. Copies `bin\nexo.exe` to `$HOME\.local\bin\nexo.exe`.
2. Adds execute/access bits as needed by Windows file ACL defaults.
3. Prints PATH guidance if `$HOME\.local\bin` is not currently on PATH.

## Verify

```powershell
nexo --help
```
"@ | Set-Content -Path $readmePath -NoNewline

    $artifactName = "$bundleName-$Version.zip"
    $artifactPath = Join-Path $OutputDir $artifactName
    if (Test-Path $artifactPath) {
        Remove-Item $artifactPath -Force
    }

    Compress-Archive -Path (Join-Path $bundleDir "*") -DestinationPath $artifactPath
    Write-Host "Created: $artifactPath"
}
finally {
    Remove-Item -Path $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $workDir -Recurse -Force -ErrorAction SilentlyContinue
}

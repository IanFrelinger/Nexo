#Requires -Version 5.1
<#
.SYNOPSIS
  Restore the Ashlar setup-gate project graph inside Linux dotnet/sdk:10.0 (no host SDK required).

  Uses ONE container so NuGet packages stay in the same filesystem as the build.
  Optional: -Build to compile Ashlar.CLI after restore.

.PARAMETER RepoRoot
  Path to Ashlar repository root (contains application/src/Ashlar.CLI).

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\docker-restore.ps1
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\docker-restore.ps1 -Build
#>
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch] $Build,
    [string] $NuGetVolume = "ashlar-nuget-packages"
)

$ErrorActionPreference = "Stop"
$image = "mcr.microsoft.com/dotnet/sdk:10.0"

docker pull $image 2>&1 | Out-Host

$buildLine = if ($Build) {
    # Matches .docker/Dockerfile.cli — avoids copy-assemblies helper needing net8 runtime in slim SDK images.
    "dotnet build application/src/Ashlar.CLI/Ashlar.CLI.csproj --no-restore -c Release -v minimal -p:SkipCopyAssemblies=true"
} else {
    "true"
}

$bashCmd = @"
set -e
dotnet restore src/Ashlar.Core.Application/Ashlar.Core.Application.csproj --verbosity minimal
dotnet restore src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj --verbosity minimal
dotnet restore application/src/Ashlar.CLI/Ashlar.CLI.csproj --verbosity minimal
dotnet restore src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj --verbosity minimal
dotnet restore src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj --verbosity minimal
$buildLine
echo "docker-restore: ok"
"@

docker run --rm `
    -v "${RepoRoot}:/src" `
    -v "${NuGetVolume}:/root/.nuget/packages" `
    -w /src `
    $image bash -lc $bashCmd
if ($LASTEXITCODE -ne 0) { throw "docker restore failed: $LASTEXITCODE" }

#requires -Version 7.0
# Optional: $env:NEXO_SDK_PACKAGE_FEED = folder of *.nupkg to skip pack-nexo-hosting-graph (same layout as after pack + Client/Sdk).
# By default sets NUGET_PACKAGES + DOTNET_CLI_HOME to a fresh temp dir (first-time consumer). Set NEXO_SDK_VERIFY_NO_ISOLATED_CACHE=1 to skip.
param(
    [string] $Version = $env:NEXO_SDK_PACKAGE_VERSION
)

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "1.0.0-ci"
}

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Feed = $env:NEXO_SDK_PACKAGE_FEED
$Out = Join-Path $Root "artifacts/nuget-verify/packages"
$CfgDir = Join-Path $Root "artifacts/nuget-verify"
$Cfg = Join-Path $CfgDir "NuGet.Config"

if (-not [string]::IsNullOrWhiteSpace($Feed)) {
    if (-not (Test-Path -LiteralPath $Feed -PathType Container)) {
        throw "NEXO_SDK_PACKAGE_FEED is not a directory: $Feed"
    }
    $Out = (Resolve-Path -LiteralPath $Feed).Path
    Write-Host "Using pre-packed feed at $Out (version $Version); skipping pack-nexo-hosting-graph."
}
else {
    if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
    New-Item -ItemType Directory -Path $Out -Force | Out-Null
    Write-Host "Packing Nexo.Hosting dependency graph as version $Version..."
    & (Join-Path $Root "scripts/pack-nexo-hosting-graph.ps1") -Version $Version -OutputDir $Out
}

New-Item -ItemType Directory -Path $CfgDir -Force | Out-Null

$isolCleanup = $null
if ([string]::IsNullOrWhiteSpace($env:NEXO_SDK_VERIFY_NO_ISOLATED_CACHE)) {
    $isolBase = if (-not [string]::IsNullOrWhiteSpace($env:NEXO_SDK_VERIFY_ISOLATED_ROOT)) {
        New-Item -ItemType Directory -Path $env:NEXO_SDK_VERIFY_ISOLATED_ROOT -Force | Out-Null
        $env:NEXO_SDK_VERIFY_ISOLATED_ROOT
    } else {
        $p = Join-Path $CfgDir ("isolated-" + [Guid]::NewGuid().ToString("n"))
        New-Item -ItemType Directory -Path $p -Force | Out-Null
        $isolCleanup = $p
        $p
    }
    $pkgDir = Join-Path $isolBase "packages"
    $cliHome = Join-Path $isolBase "cli-home"
    New-Item -ItemType Directory -Path $pkgDir -Force | Out-Null
    New-Item -ItemType Directory -Path $cliHome -Force | Out-Null
    $env:NUGET_PACKAGES = $pkgDir
    $env:DOTNET_CLI_HOME = $cliHome
    Write-Host "Isolated restore: NUGET_PACKAGES=$($env:NUGET_PACKAGES) DOTNET_CLI_HOME=$($env:DOTNET_CLI_HOME)"
} else {
    Write-Host "Skipping isolated package cache (NEXO_SDK_VERIFY_NO_ISOLATED_CACHE is set)."
}

$outUri = ([Uri]$Out).AbsoluteUri.TrimEnd('/')
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nexo-local" value="$outUri" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@ | Set-Content -Path $Cfg -Encoding utf8

Write-Host "Restoring and building package-consumption sample (NexoSdkPackageVersion=$Version)..."
dotnet restore (Join-Path $Root "docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj") `
    --configfile $Cfg `
    --force-evaluate `
    -p:NexoSdkPackageVersion=$Version `
    -v minimal

dotnet build (Join-Path $Root "docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj") `
    -c Release `
    --no-restore `
    -v minimal

Write-Host "Running package-consumption sample..."
dotnet run --project (Join-Path $Root "docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj") `
    -c Release `
    --no-build

if ($null -ne $isolCleanup -and (Test-Path -LiteralPath $isolCleanup)) {
    Remove-Item -LiteralPath $isolCleanup -Recurse -Force
}

Write-Host "verify-stable-sdk-host-sample-packages: OK"

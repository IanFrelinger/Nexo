#requires -Version 7.0
param(
    [string] $Version = $env:NEXO_SDK_PACKAGE_VERSION
)

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "1.0.0-ci"
}

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Out = Join-Path $Root "artifacts/nuget-verify/packages"
$CfgDir = Join-Path $Root "artifacts/nuget-verify"
$Cfg = Join-Path $CfgDir "NuGet.Config"

if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
New-Item -ItemType Directory -Path $Out -Force | Out-Null
New-Item -ItemType Directory -Path $CfgDir -Force | Out-Null

Write-Host "Packing Nexo.Hosting dependency graph as version $Version..."
& (Join-Path $Root "scripts/pack-nexo-hosting-graph.ps1") -Version $Version -OutputDir $Out

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

Write-Host "verify-stable-sdk-host-sample-packages: OK"

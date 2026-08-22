#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,
    [string] $FeedUrl = "https://api.nuget.org/v3/index.json",
    [string] $SourceKey = $(if ($env:ASHLAR_VERIFY_SOURCE_KEY) { $env:ASHLAR_VERIFY_SOURCE_KEY } else { "published" })
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$WorkDir = Join-Path $Root "artifacts/nuget-published-verify"
$Cfg = Join-Path $WorkDir "NuGet.Config"

if (Test-Path $WorkDir) { Remove-Item -Recurse -Force $WorkDir }
New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null

$user = $env:ASHLAR_NUGET_USERNAME
$pass = $env:ASHLAR_NUGET_PASSWORD
$credBlock = ""
if (-not [string]::IsNullOrWhiteSpace($user) -and -not [string]::IsNullOrWhiteSpace($pass)) {
    $credBlock = @"
  <packageSourceCredentials>
    <$SourceKey>
      <add key="Username" value="$([System.Security.SecurityElement]::Escape($user))" />
      <add key="ClearTextPassword" value="$([System.Security.SecurityElement]::Escape($pass))" />
    </$SourceKey>
  </packageSourceCredentials>
"@
}

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="$SourceKey" value="$FeedUrl" protocolVersion="3" />
  </packageSources>
$credBlock
</configuration>
"@ | Set-Content -Path $Cfg -Encoding utf8

$Proj = Join-Path $Root "docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj"

Write-Host "verify-published-feed: restore $Version from $FeedUrl ..."
dotnet restore $Proj --configfile $Cfg -p:AshlarSdkPackageVersion=$Version -v minimal

Write-Host "verify-published-feed: build ..."
dotnet build $Proj -c Release --no-restore -v minimal

Write-Host "verify-published-feed: run ..."
dotnet run --project $Proj -c Release --no-build

Write-Host "verify-stable-sdk-host-sample-published-feed: OK"

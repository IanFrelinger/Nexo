#requires -Version 7.0
# Align Pack-Project list with MSBuild closure from Ashlar.Hosting (+ optional scripts/pack-ashlar-hosting-graph.allowlist.txt):
#   python3 scripts/verify-pack-ashlar-hosting-graph-alignment.py
param(
    [Parameter(Mandatory = $true)][string] $Version,
    [string] $OutputDir = ""
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $Root "artifacts/nuget-local"
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

function Pack-Project([string] $RelativePath) {
    Write-Host "==> dotnet pack $RelativePath"
    dotnet pack (Join-Path $Root $RelativePath) `
        -c Release `
        -o $OutputDir `
        -p:PackageVersion=$Version `
        -v minimal
}

Pack-Project "src/Ashlar.Abstractions/Ashlar.Abstractions.csproj"
Pack-Project "src/Ashlar.Contracts/Ashlar.Contracts.csproj"
Pack-Project "src/Ashlar.Core.Domain/Ashlar.Core.Domain.csproj"
Pack-Project "src/Ashlar.Core.Application/Ashlar.Core.Application.csproj"
Pack-Project "src/Ashlar.Brick.Contracts/Ashlar.Brick.Contracts.csproj"
Pack-Project "src/Ashlar.Analyzers/Ashlar.Analyzers.csproj"
Pack-Project "src/Ashlar.Policies/Ashlar.Policies.csproj"
Pack-Project "src/Ashlar.Tools.Assembly/Ashlar.Tools.Assembly.csproj"
Pack-Project "src/Ashlar.Tools.Dev/Ashlar.Tools.Dev.csproj"
Pack-Project "src/Ashlar.Transport.Grpc/Ashlar.Transport.Grpc.csproj"
Pack-Project "src/Ashlar.Runtime/Ashlar.Runtime.csproj"
Pack-Project "src/Ashlar.Certification.Contracts/Ashlar.Certification.Contracts.csproj"
Pack-Project "src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj"
Pack-Project "src/Ashlar.Orchestration/Ashlar.Orchestration.csproj"
Pack-Project "src/Ashlar.BackgroundAgents/Ashlar.BackgroundAgents.csproj"
Pack-Project "src/Ashlar.AI.Pipeline/Ashlar.AI.Pipeline.csproj"
Pack-Project "src/Ashlar.Hosting/Ashlar.Hosting.csproj"

$cfg = Join-Path $OutputDir "PackBundle.NuGet.Config"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="ashlar-graph-local" value="$OutputDir" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@ | Set-Content -Path $cfg -Encoding utf8

Write-Host "==> dotnet pack src/Ashlar.Hosting.Bundle/Ashlar.Hosting.Bundle.csproj"
dotnet pack (Join-Path $Root "src/Ashlar.Hosting.Bundle/Ashlar.Hosting.Bundle.csproj") `
    -c Release `
    -o $OutputDir `
    -p:PackageVersion=$Version `
    --configfile $cfg `
    -v minimal

Write-Host "pack-ashlar-hosting-graph: OK ($OutputDir, version $Version)"

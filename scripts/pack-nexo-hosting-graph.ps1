#requires -Version 7.0
# Align Pack-Project list with MSBuild closure from Nexo.Hosting (+ optional scripts/pack-nexo-hosting-graph.allowlist.txt):
#   python3 scripts/verify-pack-nexo-hosting-graph-alignment.py
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

Pack-Project "src/Nexo.Abstractions/Nexo.Abstractions.csproj"
Pack-Project "src/Nexo.Contracts/Nexo.Contracts.csproj"
Pack-Project "src/Nexo.Core.Domain/Nexo.Core.Domain.csproj"
Pack-Project "src/Nexo.Core.Application/Nexo.Core.Application.csproj"
Pack-Project "src/Nexo.Brick.Contracts/Nexo.Brick.Contracts.csproj"
Pack-Project "src/Nexo.Policies/Nexo.Policies.csproj"
Pack-Project "src/Nexo.Tools.Assembly/Nexo.Tools.Assembly.csproj"
Pack-Project "src/Nexo.Tools.Dev/Nexo.Tools.Dev.csproj"
Pack-Project "src/Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj"
Pack-Project "src/Nexo.Runtime/Nexo.Runtime.csproj"
Pack-Project "src/Nexo.Certification.Contracts/Nexo.Certification.Contracts.csproj"
Pack-Project "src/Nexo.Certification.Physical/Nexo.Certification.Physical.csproj"
Pack-Project "src/Nexo.Infrastructure/Nexo.Infrastructure.csproj"
Pack-Project "src/Nexo.Orchestration/Nexo.Orchestration.csproj"
Pack-Project "src/Nexo.BackgroundAgents/Nexo.BackgroundAgents.csproj"
Pack-Project "src/Nexo.AI.Pipeline/Nexo.AI.Pipeline.csproj"
Pack-Project "src/Nexo.Hosting/Nexo.Hosting.csproj"

$cfg = Join-Path $OutputDir "PackBundle.NuGet.Config"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nexo-graph-local" value="$OutputDir" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@ | Set-Content -Path $cfg -Encoding utf8

Write-Host "==> dotnet pack src/Nexo.Hosting.Bundle/Nexo.Hosting.Bundle.csproj"
dotnet pack (Join-Path $Root "src/Nexo.Hosting.Bundle/Nexo.Hosting.Bundle.csproj") `
    -c Release `
    -o $OutputDir `
    -p:PackageVersion=$Version `
    --configfile $cfg `
    -v minimal

Write-Host "pack-nexo-hosting-graph: OK ($OutputDir, version $Version)"

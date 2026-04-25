#requires -Version 7.0
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
Pack-Project "src/Nexo.Core.Domain/Nexo.Core.Domain.csproj"
Pack-Project "src/Nexo.Core/Nexo.Core.csproj"
Pack-Project "src/Nexo.Core.Application/Nexo.Core.Application.csproj"
Pack-Project "src/Nexo.Brick.Contracts/Nexo.Brick.Contracts.csproj"
Pack-Project "src/Nexo.Policies/Nexo.Policies.csproj"
Pack-Project "src/Nexo.Tools.Assembly/Nexo.Tools.Assembly.csproj"
Pack-Project "src/Nexo.Tools.Dev/Nexo.Tools.Dev.csproj"
Pack-Project "src/Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj"
Pack-Project "src/Nexo.Runtime/Nexo.Runtime.csproj"
Pack-Project "src/Nexo.Infrastructure/Nexo.Infrastructure.csproj"
Pack-Project "src/Nexo.Orchestration/Nexo.Orchestration.csproj"
Pack-Project "src/Nexo.BackgroundAgents/Nexo.BackgroundAgents.csproj"
Pack-Project "src/Nexo.Hosting/Nexo.Hosting.csproj"

Write-Host "pack-nexo-hosting-graph: OK ($OutputDir, version $Version)"

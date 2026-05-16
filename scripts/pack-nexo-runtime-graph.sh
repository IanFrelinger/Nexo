#!/usr/bin/env bash
# Packs the Nexo.* graph for Nexo.Runtime.Bundle (kernel without Nexo.Hosting).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:?usage: pack-nexo-runtime-graph.sh <PackageVersion> [output-dir]}"
OUT="${2:-${ROOT}/artifacts/nuget-local}"

mkdir -p "${OUT}"

pack() {
  echo "==> dotnet pack $1"
  dotnet pack "${ROOT}/$1" \
    -c Release \
    -o "${OUT}" \
    -p:PackageVersion="${VERSION}" \
    -v minimal
}

pack src/Nexo.Abstractions/Nexo.Abstractions.csproj
pack src/Nexo.Core.Domain/Nexo.Core.Domain.csproj
pack src/Nexo.Core/Nexo.Core.csproj
pack src/Nexo.Core.Application/Nexo.Core.Application.csproj
pack src/Nexo.Brick.Contracts/Nexo.Brick.Contracts.csproj
pack src/Nexo.Policies/Nexo.Policies.csproj
pack src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj
pack src/Nexo.Tools.Assembly/Nexo.Tools.Assembly.csproj
pack src/Nexo.Tools.Dev/Nexo.Tools.Dev.csproj
pack src/Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj
pack src/Nexo.Runtime/Nexo.Runtime.csproj
pack src/Nexo.Infrastructure/Nexo.Infrastructure.csproj
pack src/Nexo.Orchestration/Nexo.Orchestration.csproj
pack src/Nexo.BackgroundAgents/Nexo.BackgroundAgents.csproj

CFG="${OUT}/PackRuntimeBundle.NuGet.Config"
cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nexo-graph-local" value="${OUT}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo "==> dotnet pack src/Nexo.Runtime.Bundle/Nexo.Runtime.Bundle.csproj"
dotnet pack "${ROOT}/src/Nexo.Runtime.Bundle/Nexo.Runtime.Bundle.csproj" \
  -c Release \
  -o "${OUT}" \
  -p:PackageVersion="${VERSION}" \
  --configfile "${CFG}" \
  -v minimal

echo "pack-nexo-runtime-graph: OK (${OUT}, version ${VERSION})"

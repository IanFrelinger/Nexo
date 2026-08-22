#!/usr/bin/env bash
# Packs the Ashlar.* graph for Ashlar.Runtime.Bundle (kernel without Ashlar.Hosting).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:?usage: pack-ashlar-runtime-graph.sh <PackageVersion> [output-dir]}"
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

pack src/Ashlar.Abstractions/Ashlar.Abstractions.csproj
pack src/Ashlar.Core.Domain/Ashlar.Core.Domain.csproj
pack src/Ashlar.Core.Application/Ashlar.Core.Application.csproj
pack src/Ashlar.Brick.Contracts/Ashlar.Brick.Contracts.csproj
pack src/Ashlar.Policies/Ashlar.Policies.csproj
pack src/Ashlar.Policies.Dev/Ashlar.Policies.Dev.csproj
pack src/Ashlar.Tools.Assembly/Ashlar.Tools.Assembly.csproj
pack src/Ashlar.Tools.Dev/Ashlar.Tools.Dev.csproj
pack src/Ashlar.Transport.Grpc/Ashlar.Transport.Grpc.csproj
pack src/Ashlar.Runtime/Ashlar.Runtime.csproj
pack src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj
pack src/Ashlar.Orchestration/Ashlar.Orchestration.csproj
pack src/Ashlar.BackgroundAgents/Ashlar.BackgroundAgents.csproj

CFG="${OUT}/PackRuntimeBundle.NuGet.Config"
cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="ashlar-graph-local" value="${OUT}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo "==> dotnet pack src/Ashlar.Runtime.Bundle/Ashlar.Runtime.Bundle.csproj"
dotnet pack "${ROOT}/src/Ashlar.Runtime.Bundle/Ashlar.Runtime.Bundle.csproj" \
  -c Release \
  -o "${OUT}" \
  -p:PackageVersion="${VERSION}" \
  --configfile "${CFG}" \
  -v minimal

echo "pack-ashlar-runtime-graph: OK (${OUT}, version ${VERSION})"

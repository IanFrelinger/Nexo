#!/usr/bin/env bash
# Packs the Nexo.* project graph required for a consumable Nexo.Hosting package (same PackageVersion on all).
# Align with MSBuild closure from Nexo.Hosting (+ optional scripts/pack-nexo-hosting-graph.allowlist.txt):
#   python3 scripts/verify-pack-nexo-hosting-graph-alignment.py
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:?usage: pack-nexo-hosting-graph.sh <PackageVersion> [output-dir]}"
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
pack src/Nexo.Contracts/Nexo.Contracts.csproj
pack src/Nexo.Core.Domain/Nexo.Core.Domain.csproj
pack src/Nexo.Core.Application/Nexo.Core.Application.csproj
pack src/Nexo.Brick.Contracts/Nexo.Brick.Contracts.csproj
pack src/Nexo.Policies/Nexo.Policies.csproj
pack src/Nexo.Tools.Assembly/Nexo.Tools.Assembly.csproj
pack src/Nexo.Tools.Dev/Nexo.Tools.Dev.csproj
pack src/Nexo.Transport.Grpc/Nexo.Transport.Grpc.csproj
pack src/Nexo.Runtime/Nexo.Runtime.csproj
pack src/Nexo.Certification.Contracts/Nexo.Certification.Contracts.csproj
pack src/Nexo.Certification.Physical/Nexo.Certification.Physical.csproj
pack src/Nexo.Infrastructure/Nexo.Infrastructure.csproj
pack src/Nexo.Orchestration/Nexo.Orchestration.csproj
pack src/Nexo.BackgroundAgents/Nexo.BackgroundAgents.csproj
pack src/Nexo.AI.Pipeline/Nexo.AI.Pipeline.csproj
pack src/Nexo.Hosting/Nexo.Hosting.csproj

CFG="${OUT}/PackBundle.NuGet.Config"
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

echo "==> dotnet pack src/Nexo.Hosting.Bundle/Nexo.Hosting.Bundle.csproj"
dotnet pack "${ROOT}/src/Nexo.Hosting.Bundle/Nexo.Hosting.Bundle.csproj" \
  -c Release \
  -o "${OUT}" \
  -p:PackageVersion="${VERSION}" \
  --configfile "${CFG}" \
  -v minimal

echo "pack-nexo-hosting-graph: OK (${OUT}, version ${VERSION})"

#!/usr/bin/env bash
# Pack certified state log contracts + phase-witness bundle for Project C reuse.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${NEXO_CERTIFIED_STATE_REUSE_VERSION:-0.1.0}"
FEED="${NEXO_CERTIFIED_STATE_REUSE_FEED:-${ROOT}/artifacts/certified-state-log-feed}"
WITNESS_DIR="${ROOT}/samples/certified-state-log-reuse/Nexo.Certified.PhaseWitness"
CFG="${FEED}/NuGet.Config"

mkdir -p "${FEED}"

pack() {
  dotnet pack "${ROOT}/${1}" -c Release -o "${FEED}" \
    -p:PackageVersion="${VERSION}" \
    -p:IncludeTestProjectReferences=false \
    -p:UseProjectReferences=false \
    -v minimal
}

echo "==> Pack state binding contracts to ${FEED}"
pack src/Nexo.Certification.Contracts/Nexo.Certification.Contracts.csproj
pack src/Nexo.Certification.State/Nexo.Certification.State.csproj

cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="certified-state-feed" value="${FEED}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

echo "==> Regenerate bundled phase-witness artifacts"
dotnet run --project "${ROOT}/tools/GenStateLogFixtures/GenStateLogFixtures.csproj" -- "${WITNESS_DIR}"

echo "==> Feed ready at ${FEED} (NuGet.Config=${CFG})"
echo "Project C consumes Nexo.Certification.State + bundled JSON under ${WITNESS_DIR}"

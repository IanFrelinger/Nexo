#!/usr/bin/env bash
# Restore docs/samples/NugetOrgRestoreVerify against nuget.org only (validates published graph + transitive resolution).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VER="${ASHLAR_NUGET_RESTORE_VERIFY_VERSION:?set ASHLAR_NUGET_RESTORE_VERIFY_VERSION (semver, no v prefix)}"
VER="${VER#v}"
CFG_DIR="${ROOT}/artifacts/nuget-org-restore-verify"
CFG="${CFG_DIR}/NuGet.Config"
mkdir -p "${CFG_DIR}"

cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo "dotnet restore (nuget.org only) Ashlar.Hosting.Bundle @ ${VER}..."
dotnet restore "${ROOT}/docs/samples/NugetOrgRestoreVerify/Ashlar.NugetOrgRestoreVerify.csproj" \
  --configfile "${CFG}" \
  --force-evaluate \
  -p:AshlarPublishedVerifyVersion="${VER}" \
  -v minimal

echo "verify-nuget-org-restore-published-version: OK"

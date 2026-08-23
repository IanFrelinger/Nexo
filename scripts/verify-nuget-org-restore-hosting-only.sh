#!/usr/bin/env bash
# Same as verify-nuget-org-restore-published-version but for Ashlar.NugetOrgRestoreHostingOnly (direct Ashlar.Hosting ref).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VER="${ASHLAR_NUGET_RESTORE_VERIFY_VERSION:?set ASHLAR_NUGET_RESTORE_VERIFY_VERSION}"
VER="${VER#v}"
CFG_DIR="${ROOT}/artifacts/nuget-org-restore-hosting-only"
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
dotnet restore "${ROOT}/docs/samples/NugetOrgRestoreHostingOnly/Ashlar.NugetOrgRestoreHostingOnly.csproj" \
  --configfile "${CFG}" \
  --force-evaluate \
  -p:AshlarPublishedVerifyVersion="${VER}" \
  -v minimal
echo "verify-nuget-org-restore-hosting-only: OK"

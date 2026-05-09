#!/usr/bin/env bash
# Restore StableSdkHostSample.Package against a published NuGet feed only (no local pack).
# Use after packages appear on nuget.org or GitHub Packages to validate install + bootstrap.
#
# Usage:
#   bash scripts/verify-stable-sdk-host-sample-published-feed.sh 1.2.3
#   bash scripts/verify-stable-sdk-host-sample-published-feed.sh 1.2.3 https://api.nuget.org/v3/index.json
#
# Environment:
#   NEXO_NUGET_USERNAME / NEXO_NUGET_PASSWORD — optional; if both set, added to NuGet.Config
#     packageSourceCredentials for the single feed (e.g. GitHub Packages PAT as password).
#
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:?usage: verify-stable-sdk-host-sample-published-feed.sh <PackageVersion> [feed-url]}"
FEED_URL="${2:-https://api.nuget.org/v3/index.json}"
SOURCE_KEY="${NEXO_VERIFY_SOURCE_KEY:-published}"

WORKDIR="${ROOT}/artifacts/nuget-published-verify"
CFG="${WORKDIR}/NuGet.Config"
rm -rf "${WORKDIR}"
mkdir -p "${WORKDIR}"

if [[ -n "${NEXO_NUGET_USERNAME:-}" && -n "${NEXO_NUGET_PASSWORD:-}" ]]; then
  cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="${SOURCE_KEY}" value="${FEED_URL}" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <${SOURCE_KEY}>
      <add key="Username" value="${NEXO_NUGET_USERNAME}" />
      <add key="ClearTextPassword" value="${NEXO_NUGET_PASSWORD}" />
    </${SOURCE_KEY}>
  </packageSourceCredentials>
</configuration>
EOF
else
  cat > "${CFG}" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="${SOURCE_KEY}" value="${FEED_URL}" protocolVersion="3" />
  </packageSources>
</configuration>
EOF
fi

PROJ="${ROOT}/docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj"

echo "verify-published-feed: restore ${VERSION} from ${FEED_URL} ..."
dotnet restore "${PROJ}" \
  --configfile "${CFG}" \
  -p:NexoSdkPackageVersion="${VERSION}" \
  -v minimal

echo "verify-published-feed: build ..."
dotnet build "${PROJ}" \
  -c Release \
  --no-restore \
  -v minimal

echo "verify-published-feed: run ..."
dotnet run --project "${PROJ}" \
  -c Release \
  --no-build

echo "verify-stable-sdk-host-sample-published-feed: OK"

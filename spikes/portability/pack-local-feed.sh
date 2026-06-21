#!/usr/bin/env bash
# Step 3: pack consumer-surface packages at VERSION (default 0.1.0) into a local feed.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
VERSION="${NEXO_PORTABILITY_PACK_VERSION:-$(tr -d '[:space:]' < "${ROOT}/VERSION")}"
FEED="${NEXO_PORTABILITY_PACK_FEED:-${ROOT}/artifacts/nuget-local-portability}"

mkdir -p "${FEED}"

pack() {
  local project="$1"
  echo "==> dotnet pack ${project}"
  dotnet pack "${ROOT}/${project}" \
    -c Release \
    -o "${FEED}" \
    -p:PackageVersion="${VERSION}" \
    -p:IncludeTestProjectReferences=false \
    -v minimal
}

echo "==> Packing portability feed as version ${VERSION} into ${FEED}"
bash "${ROOT}/scripts/pack-nexo-hosting-graph.sh" "${VERSION}" "${FEED}"
pack src/Nexo.Authoring/Nexo.Authoring.csproj
pack src/Nexo.Sdk/Nexo.Sdk.csproj
pack src/Nexo.Client/Nexo.Client.csproj

echo "pack-local-feed: OK (${FEED})"

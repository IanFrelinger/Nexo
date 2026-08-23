#!/usr/bin/env bash
# Step 3: pack consumer-surface packages at VERSION (default 0.1.0) into a local feed.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
VERSION="${ASHLAR_PORTABILITY_PACK_VERSION:-$(tr -d '[:space:]' < "${ROOT}/VERSION")}"
FEED="${ASHLAR_PORTABILITY_PACK_FEED:-${ROOT}/artifacts/nuget-local-portability}"

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
bash "${ROOT}/scripts/pack-ashlar-hosting-graph.sh" "${VERSION}" "${FEED}"
pack src/Ashlar.Authoring/Ashlar.Authoring.csproj
pack src/Ashlar.Sdk/Ashlar.Sdk.csproj
pack src/Ashlar.Client/Ashlar.Client.csproj

echo "pack-local-feed: OK (${FEED})"

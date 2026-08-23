#!/usr/bin/env bash
# Step 1: invoke INewBrickGenerator for ErrorSummaryExtractor and capture artifacts.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="${ASHLAR_PORTABILITY_GENERATED_DIR:-${ROOT}/spikes/portability/generated}"
VERSION="$(tr -d '[:space:]' < "${ROOT}/VERSION")"

mkdir -p "${OUT}"

echo "==> Generating ErrorSummaryExtractor probe brick (version ${VERSION})"
dotnet run --project "${ROOT}/spikes/portability/tools/GenerateProbeBrick/GenerateProbeBrick.csproj" \
  -c Release \
  -- "${OUT}" "${VERSION}"

if [[ ! -f "${OUT}/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs" ]]; then
  echo "Missing generated brick source." >&2
  exit 1
fi

if [[ ! -f "${OUT}/manifest.json" ]]; then
  echo "Missing manifest.json certification artifact." >&2
  exit 1
fi

echo "generate-probe-brick: OK (${OUT})"

#!/usr/bin/env bash
# Step 2: certify the probe brick through the S0–S2 certification gate.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="${ASHLAR_PORTABILITY_GENERATED_DIR:-${ROOT}/spikes/portability/generated}"
RECORD="${OUT}/certification-record.json"
VERSION="${ASHLAR_PORTABILITY_PACK_VERSION:-$(tr -d '[:space:]' < "${ROOT}/VERSION")}"
FEED="${ASHLAR_PORTABILITY_PACK_FEED:-${ROOT}/artifacts/nuget-local-portability}"
WITNESS="${ROOT}/spikes/portability/witness/error-summary-extractor.witness.json"
GATE="${ROOT}/scripts/certify-brick-gate.sh"

mkdir -p "${OUT}"

if [[ ! -x "${GATE}" && ! -f "${GATE}" ]]; then
  timestamp="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  cat > "${RECORD}" <<EOF
{
  "status": "FAIL",
  "stage": "S0-S2",
  "admitted": false,
  "signed": false,
  "timestamp": "${timestamp}",
  "reason": "No S0-S2 brick certification gate script found in repository",
  "brickId": "error-summary-extractor"
}
EOF
  echo "certify-probe-brick: FAIL — certification gate not present (see ${RECORD})" >&2
  exit 1
fi

echo "==> Packing local feed for certification restore @ ${VERSION}"
mkdir -p "${FEED}"
bash "${ROOT}/scripts/pack-ashlar-hosting-graph.sh" "${VERSION}" "${FEED}"
dotnet pack "${ROOT}/src/Ashlar.Authoring/Ashlar.Authoring.csproj" \
  -c Release \
  -o "${FEED}" \
  -p:PackageVersion="${VERSION}" \
  --configfile "${FEED}/PackBundle.NuGet.Config" \
  -v minimal

export ASHLAR_CERT_NUGET_CONFIG="${FEED}/PackBundle.NuGet.Config"

echo "==> Running certification gate: ${GATE}"
if bash "${GATE}" "${OUT}/ErrorSummaryExtractorBrick" "${WITNESS}" "${RECORD}"; then
  echo "certify-probe-brick: OK (record at ${RECORD})"
else
  echo "certify-probe-brick: FAIL — gate rejected brick (see ${RECORD})" >&2
  exit 1
fi

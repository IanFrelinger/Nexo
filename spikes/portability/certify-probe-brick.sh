#!/usr/bin/env bash
# Step 2: attempt to certify the probe brick through the S0–S2 certification gate.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="${NEXO_PORTABILITY_GENERATED_DIR:-${ROOT}/spikes/portability/generated}"
RECORD="${OUT}/certification-record.json"

mkdir -p "${OUT}"

# Known certification gate entry points (S0–S2 harness). None found in repo at spike time.
GATE_CANDIDATES=(
  "scripts/certify-brick-gate.sh"
  "scripts/verify-brick-certification-gate.sh"
  "scripts/verify-brick-certification-s0-s2.sh"
)

FOUND_GATE=""
for candidate in "${GATE_CANDIDATES[@]}"; do
  if [[ -f "${ROOT}/${candidate}" ]]; then
    FOUND_GATE="${ROOT}/${candidate}"
    break
  fi
done

timestamp="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

if [[ -z "${FOUND_GATE}" ]]; then
  cat > "${RECORD}" <<EOF
{
  "status": "FAIL",
  "stage": "S0-S2",
  "admitted": false,
  "signed": false,
  "timestamp": "${timestamp}",
  "reason": "No S0-S2 brick certification gate script found in repository",
  "searched": $(printf '%s\n' "${GATE_CANDIDATES[@]}" | jq -R . | jq -s .),
  "brickId": "error-summary-extractor"
}
EOF
  echo "certify-probe-brick: FAIL — S0-S2 certification gate not present (see ${RECORD})" >&2
  exit 1
fi

echo "==> Running certification gate: ${FOUND_GATE}"
if bash "${FOUND_GATE}" "${OUT}/ErrorSummaryExtractorBrick"; then
  cat > "${RECORD}" <<EOF
{
  "status": "PASS",
  "stage": "S0-S2",
  "admitted": true,
  "signed": true,
  "timestamp": "${timestamp}",
  "gate": "${FOUND_GATE}",
  "brickId": "error-summary-extractor"
}
EOF
  echo "certify-probe-brick: OK"
else
  cat > "${RECORD}" <<EOF
{
  "status": "FAIL",
  "stage": "S0-S2",
  "admitted": false,
  "signed": false,
  "timestamp": "${timestamp}",
  "gate": "${FOUND_GATE}",
  "brickId": "error-summary-extractor"
}
EOF
  echo "certify-probe-brick: FAIL — gate rejected brick (see ${RECORD})" >&2
  exit 1
fi

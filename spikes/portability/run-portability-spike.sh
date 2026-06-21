#!/usr/bin/env bash
# Orchestrates atomic brick portability spike (steps 1–5).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
VERSION="$(tr -d '[:space:]' < "${ROOT}/VERSION")"
GENERATED="${ROOT}/spikes/portability/generated"
REPORT="${ROOT}/spikes/portability/REPORT.md"

step1_status="PENDING"
step2_status="PENDING"
step3_status="PENDING"
step4_status="PENDING"
step5_status="PENDING"
step2_detail=""
step5_detail=""
leaks=()

record_leak() { leaks+=("$1"); }

echo "==> Step 1: generate probe brick"
if bash "${ROOT}/spikes/portability/generate-probe-brick.sh"; then
  step1_status="PASS"
else
  step1_status="FAIL"
fi

echo "==> Step 2: certify probe brick"
set +e
bash "${ROOT}/spikes/portability/certify-probe-brick.sh"
cert_exit=$?
set -e
if [[ "${cert_exit}" -eq 0 ]]; then
  step2_status="PASS"
else
  step2_status="FAIL"
  step2_detail="S0–S2 certification gate script not found in repository (see generated/certification-record.json)"
fi

echo "==> Steps 3–5: pack ${VERSION}, consume externally, assert execution"
set +e
NEXO_EXTERNAL_PRODUCT_VERIFY_VERSION="${VERSION}" \
NEXO_EXTERNAL_PRODUCT_PROBE_BRICK=1 \
NEXO_EXTERNAL_PRODUCT_PROBE_BRICK_SOURCE="${GENERATED}/ErrorSummaryExtractorBrick" \
  bash "${ROOT}/scripts/verify-external-product-shape.sh"
verify_exit=$?
set -e

if [[ "${verify_exit}" -eq 0 ]]; then
  step3_status="PASS"
  step4_status="PASS"
  step5_status="PASS"
else
  step3_status="UNKNOWN"
  step4_status="UNKNOWN"
  step5_status="FAIL"
  step5_detail="verify-external-product-shape.sh exited ${verify_exit}"
fi

# Scan generated brick for repo-internal leaks
if [[ -f "${GENERATED}/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs" ]]; then
  if rg -q "Nexo\\.Infrastructure|Nexo\\.Core\\.Application|ProjectReference|src/Nexo" "${GENERATED}/ErrorSummaryExtractorBrick"; then
    record_leak "Generated source references repo-internal namespaces or paths"
  fi
  if rg -q "using Nexo\\.Core\\.Domain" "${GENERATED}/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs"; then
    record_leak "Generated brick uses Nexo.Core.Domain.* namespaces (shipped via Nexo.Authoring/Nexo.Brick.Contracts but not the pinned package IDs)"
  fi
fi

if [[ -f "${GENERATED}/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.csproj" ]]; then
  if rg -q "ProjectReference" "${GENERATED}/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.csproj"; then
    record_leak "Brick csproj contains ProjectReference to repo-internal projects"
  fi
fi

mkdir -p "$(dirname "${REPORT}")"
{
  echo "# Atomic Brick Portability Spike Report"
  echo
  echo "Version pin: \`${VERSION}\` (from \`VERSION\`)"
  echo "Probe brick: \`ErrorSummaryExtractor\` (deterministic log scanner)"
  echo
  echo "## Step results"
  echo
  echo "| Step | Description | Result |"
  echo "|------|-------------|--------|"
  echo "| 1 | Generate deterministic probe brick via \`INewBrickGenerator\` | ${step1_status} |"
  echo "| 2 | Certify through S0–S2 gate (signed admission record) | ${step2_status} |"
  echo "| 3 | Pack Nexo.Brick.Contracts + Nexo.Authoring (+ Hosting.Bundle) @ ${VERSION} | ${step3_status} |"
  echo "| 4 | Consume generated brick from external template (package pins only) | ${step4_status} |"
  echo "| 5 | Cross-project HTTP execute assertion | ${step5_status} |"
  echo
  if [[ -n "${step2_detail}" ]]; then
    echo "**Step 2 detail:** ${step2_detail}"
    echo
  fi
  if [[ -n "${step5_detail}" ]]; then
    echo "**Step 5 detail:** ${step5_detail}"
    echo
  fi
  echo "## Contract-stability gaps (repo-internal context in generated brick)"
  echo
  if [[ "${#leaks[@]}" -eq 0 ]]; then
    echo "- None detected in generated source/csproj (namespaces come from published authoring surface)."
  else
    for leak in "${leaks[@]}"; do
      echo "- ${leak}"
    done
  fi
  echo
  echo "## Artifacts"
  echo
  echo "- \`spikes/portability/generated/ErrorSummaryExtractorBrick/ErrorSummaryExtractorBrick.cs\`"
  echo "- \`spikes/portability/generated/manifest.json\`"
  echo "- \`spikes/portability/generated/certification-record.json\`"
} > "${REPORT}"

cat "${REPORT}"

if [[ "${step1_status}" != "PASS" || "${step5_status}" != "PASS" ]]; then
  exit 1
fi

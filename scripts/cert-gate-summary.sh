#!/usr/bin/env bash
# Write generation-safety results to GITHUB_STEP_SUMMARY (or stdout locally).
set -euo pipefail

TRX="${1:-test-results/cert-gate.trx}"
SUMMARY="${GITHUB_STEP_SUMMARY:-/dev/stdout}"

if [[ ! -f "${TRX}" ]]; then
  {
    echo "## cert-gate — generation safety"
    echo ""
    echo "TRX not found (\`${TRX}\`). Tests may not have run."
  } >> "${SUMMARY}"
  exit 0
fi

outcome_for() {
  local short_name="$1"
  local line
  line="$(grep -F "testName=\"Nexo.Tests.Infrastructure.Tests.Adaptation.GenerationSafetyTests.${short_name}\"" "${TRX}" | head -1 || true)"
  if [[ -z "${line}" ]]; then
    echo "not found"
    return
  fi
  if grep -q 'outcome="Passed"' <<< "${line}"; then
    echo "✅ Pass"
  elif grep -q 'outcome="Failed"' <<< "${line}"; then
    echo "❌ Fail"
  else
    grep -oE 'outcome="[^"]+"' <<< "${line}" | head -1 | sed 's/outcome="//;s/"//'
  fi
}

EXECUTED="$(grep -oE 'executed="[0-9]+"' "${TRX}" | head -1 | sed 's/executed="//;s/"//')"
EXECUTED="${EXECUTED:-0}"

{
  echo "## cert-gate — generation safety"
  echo ""
  echo "| Test | Result |"
  echo "|------|--------|"
  echo "| **KEY: gate catches a wrong generation** — \`BuggyGeneration_StrongWitness_Rejects\` | $(outcome_for "BuggyGeneration_StrongWitness_Rejects") |"
  echo "| \`GoodGeneration_StrongWitness_Admits_WithZeroEscapeRate\` | $(outcome_for "GoodGeneration_StrongWitness_Admits_WithZeroEscapeRate") |"
  echo "| \`GoodGeneration_WeakWitness_Rejects_WithTeeth\` | $(outcome_for "GoodGeneration_WeakWitness_Rejects_WithTeeth") |"
  echo "| \`DependencyLeakGeneration_Rejects_DependencyCheck\` | $(outcome_for "DependencyLeakGeneration_Rejects_DependencyCheck") |"
  echo ""
  echo "Total cert-gate tests executed: **${EXECUTED}** (expected ≥ 17; 19 when dogfood witness enabled)."
} >> "${SUMMARY}"

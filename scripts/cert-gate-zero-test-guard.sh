#!/usr/bin/env bash
# Fail closed when the cert-gate filter matches too few tests (dotnet test exits 0 on zero matches).
set -euo pipefail

# CertificationGateTeethTests: 6, AstMutationEngineTests: 2, GenerationSafetyTests: 4,
# CompositionCertificationGateTeethTests: 5, DamageResolverDogfoodTests: 2 (skipped until human witness)
# Active executed count: 17. When dogfood witness is populated and Skip removed: 19.
readonly MIN_EXPECTED=17
TRX="${1:-test-results/cert-gate.trx}"

if [[ ! -f "${TRX}" ]]; then
  echo "cert-gate TRX not found: ${TRX}"
  exit 1
fi

EXECUTED="$(grep -oE 'executed="[0-9]+"' "${TRX}" | head -1 | sed 's/executed="//;s/"//')"
EXECUTED="${EXECUTED:-0}"

if [[ "${EXECUTED}" -lt "${MIN_EXPECTED}" ]]; then
  echo "cert-gate matched fewer tests than expected (ran=${EXECUTED}, expected>=${MIN_EXPECTED}) — filter is stale."
  exit 1
fi

echo "cert-gate executed ${EXECUTED} tests (expected>=${MIN_EXPECTED})."

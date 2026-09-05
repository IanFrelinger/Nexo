#!/usr/bin/env bash
# Fail closed when the cert-gate filter matches too few tests (dotnet test exits 0 on zero matches).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/cert-gate-config.sh
source "${ROOT}/scripts/cert-gate-config.sh"

TRX="${1:-test-results/cert-gate.trx}"
MIN_EXPECTED="$(cert_gate_expected_count "${ROOT}")"

if [[ ! -f "${TRX}" ]]; then
  echo "cert-gate TRX not found: ${TRX}"
  exit 1
fi

if [[ -z "${MIN_EXPECTED}" || "${MIN_EXPECTED}" -lt 1 ]]; then
  echo "cert-gate could not derive expected test count (got=${MIN_EXPECTED:-0}) — build test project first."
  exit 1
fi

if [[ "${MIN_EXPECTED}" -lt "${CERT_GATE_MIN_TESTS}" ]]; then
  echo "cert-gate discovery collapsed (listed=${MIN_EXPECTED}, collapse-min=${CERT_GATE_MIN_TESTS}) — filter or suite shrank."
  exit 1
fi

REPORTED="$(grep -oE 'total="[0-9]+"' "${TRX}" | head -1 | sed 's/total="//;s/"//')"
REPORTED="${REPORTED:-0}"

if [[ "${REPORTED}" -lt "${MIN_EXPECTED}" ]]; then
  echo "cert-gate matched fewer tests than expected (reported=${REPORTED}, expected>=${MIN_EXPECTED}) — filter is stale."
  exit 1
fi

echo "cert-gate reported ${REPORTED} tests (expected>=${MIN_EXPECTED}, derived from --list-tests)."

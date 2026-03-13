#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="${1:-$(pwd)}"
RELEASE_GOALS="${ROOT_DIR}/docs/runtime/benchmarks/release_goals.txt"
CHAOS_GOALS="${ROOT_DIR}/docs/runtime/benchmarks/chaos_goals.txt"

MIN_PASS_RATE="${NEXO_RELEASE_MIN_PASS_RATE:-0.80}"
MIN_TOTAL="${NEXO_RELEASE_MIN_TOTAL:-3}"
HISTORY_WINDOW="${NEXO_RELEASE_HISTORY_WINDOW:-3}"
PROVIDER="${NEXO_RELEASE_PROVIDER:-mock-json}"

if [[ ! -f "${RELEASE_GOALS}" ]]; then
  echo "release gate: missing release goals file: ${RELEASE_GOALS}" >&2
  exit 1
fi

if [[ ! -f "${CHAOS_GOALS}" ]]; then
  echo "release gate: missing chaos goals file: ${CHAOS_GOALS}" >&2
  exit 1
fi

echo "=== Runtime Release Gate: release matrix ==="
dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime evaluate \
  --goals-file "${RELEASE_GOALS}" \
  --policies release \
  --repo-root "${ROOT_DIR}" \
  --provider "${PROVIDER}" \
  --allow-mock \
  --run-tests \
  --persist-history \
  --json

echo "=== Runtime Release Gate: SLO gate ==="
dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime gate \
  --repo-root "${ROOT_DIR}" \
  --policy release \
  --history-window "${HISTORY_WINDOW}" \
  --min-pass-rate "${MIN_PASS_RATE}" \
  --min-total "${MIN_TOTAL}" \
  --json

echo "=== Runtime Release Gate: chaos matrix (non-gating) ==="
set +e
dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime evaluate \
  --goals-file "${CHAOS_GOALS}" \
  --policies prod \
  --repo-root "${ROOT_DIR}" \
  --provider "${PROVIDER}" \
  --allow-mock \
  --run-tests \
  --persist-history false \
  --json
CHAOS_EXIT=$?
set -e

if [[ ${CHAOS_EXIT} -ne 0 ]]; then
  echo "release gate: chaos matrix reported failures (expected in stress mode)." >&2
fi

echo "=== Runtime Release Gate: PASSED ==="

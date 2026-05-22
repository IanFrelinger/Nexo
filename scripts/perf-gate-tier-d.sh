#!/usr/bin/env bash
# Perf Tier D: mini-soak (repeated pipeline runs). Set PERF_GATE_SOAK_MINUTES for long soak.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_PROJECT="application/src/Nexo.CLI/Nexo.CLI.csproj"
TMP_ROOT="${TMPDIR:-/tmp}/nexo-perf-soak-$$"
mkdir -p "$TMP_ROOT"
trap 'rm -rf "$TMP_ROOT"' EXIT

TEMPLATE="$TMP_ROOT/pipeline_soak.json"
SOAK_MINUTES="${PERF_GATE_SOAK_MINUTES:-0}"
ITERATIONS="${PERF_GATE_SOAK_ITERATIONS:-15}"

cat >"$TEMPLATE" <<'JSON'
{
  "templateId": "perf-soak",
  "version": "1.0",
  "stages": [{ "id": "work", "name": "Work", "mode": "Deterministic" }],
  "edges": []
}
JSON

dotnet build "$CLI_PROJECT" -v minimal >/dev/null

if [ "$SOAK_MINUTES" -gt 0 ]; then
  echo "== Perf Tier D: soak ${SOAK_MINUTES} minutes =="
  END=$(( $(date +%s) + SOAK_MINUTES * 60 ))
  RUNS=0
  FAILS=0
  while [ "$(date +%s)" -lt "$END" ]; do
    RUNS=$((RUNS + 1))
    if ! NEXO_ALLOW_MOCK=1 dotnet run --project "$CLI_PROJECT" --no-build -- \
      pipeline run --template "$TEMPLATE" --run-id "soak-$RUNS" --format-json >/dev/null 2>&1; then
      FAILS=$((FAILS + 1))
    fi
  done
else
  echo "== Perf Tier D: mini-soak ($ITERATIONS iterations) =="
  RUNS=$ITERATIONS
  FAILS=0
  for i in $(seq 1 "$ITERATIONS"); do
    if ! NEXO_ALLOW_MOCK=1 dotnet run --project "$CLI_PROJECT" --no-build -- \
      pipeline run --template "$TEMPLATE" --run-id "soak-$i" --format-json >/dev/null 2>&1; then
      FAILS=$((FAILS + 1))
    fi
  done
fi

REPORT_DIR=".nexo/perf"
mkdir -p "$REPORT_DIR"
cat >"$REPORT_DIR/soak.json" <<EOF
{
  "runs": $RUNS,
  "failures": $FAILS,
  "soakMinutes": $SOAK_MINUTES,
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF

echo "soak: runs=$RUNS failures=$FAILS"

if [ "$FAILS" -gt 0 ]; then
  echo "perf-gate-tier-d: FAIL" >&2
  exit 1
fi

echo ""
echo "perf-gate-tier-d: PASS"

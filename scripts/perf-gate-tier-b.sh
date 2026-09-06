#!/usr/bin/env bash
# Perf Tier B: pipeline throughput (mocked deterministic stages).
# ASHLAR_ALLOW_MOCK=1 enables CI-only test hook in default adapters (succeeds instead of fail-closed).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_PROJECT="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
TMP_ROOT="${TMPDIR:-/tmp}/ashlar-perf-gate-$$"
mkdir -p "$TMP_ROOT"
trap 'rm -rf "$TMP_ROOT"' EXIT

TEMPLATE="$TMP_ROOT/pipeline_perf.json"
ITERATIONS="${PERF_GATE_PIPELINE_ITERATIONS:-5}"
MAX_TOTAL_MS="${PERF_GATE_PIPELINE_MAX_MS:-180000}"

cat >"$TEMPLATE" <<'JSON'
{
  "templateId": "perf-gate",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "process", "name": "Process", "mode": "Deterministic" }
  ],
  "edges": [{ "fromStageId": "ingest", "toStageId": "process" }]
}
JSON

echo "== Perf Tier B: pipeline throughput ($ITERATIONS runs, max ${MAX_TOTAL_MS}ms total) =="
dotnet build "$CLI_PROJECT" -v minimal >/dev/null

START_MS=$(python3 -c 'import time; print(int(time.time()*1000))')
for i in $(seq 1 "$ITERATIONS"); do
  ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI_PROJECT" --no-build -- \
    pipeline run --template "$TEMPLATE" --run-id "perf-gate-$i" --format-json >/dev/null
done
END_MS=$(python3 -c 'import time; print(int(time.time()*1000))')
ELAPSED=$((END_MS - START_MS))
AVG=$((ELAPSED / ITERATIONS))

REPORT_DIR=".ashlar/perf"
mkdir -p "$REPORT_DIR"
cat >"$REPORT_DIR/pipeline-throughput.json" <<EOF
{
  "iterations": $ITERATIONS,
  "totalMs": $ELAPSED,
  "avgMsPerRun": $AVG,
  "maxTotalMs": $MAX_TOTAL_MS,
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF

echo "pipeline throughput: ${ELAPSED}ms total, ${AVG}ms avg/run"

if [ "$ELAPSED" -gt "$MAX_TOTAL_MS" ]; then
  echo "perf-gate-tier-b: FAIL (exceeded ${MAX_TOTAL_MS}ms)" >&2
  exit 1
fi

echo ""
echo "perf-gate-tier-b: PASS"

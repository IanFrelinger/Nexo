#!/usr/bin/env bash
# Perf Tier C: CLI cold-start / help smoke timing.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_PROJECT="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
MAX_MS="${PERF_GATE_COLD_START_MAX_MS:-45000}"

echo "== Perf Tier C: CLI cold-start (--help) =="
dotnet build "$CLI_PROJECT" -v minimal >/dev/null

START_MS=$(python3 -c 'import time; print(int(time.time()*1000))')
dotnet run --project "$CLI_PROJECT" --no-build -- --help >/dev/null
END_MS=$(python3 -c 'import time; print(int(time.time()*1000))')
ELAPSED=$((END_MS - START_MS))

REPORT_DIR=".ashlar/perf"
mkdir -p "$REPORT_DIR"
cat >"$REPORT_DIR/cold-start.json" <<EOF
{
  "command": "dotnet run -- --help",
  "elapsedMs": $ELAPSED,
  "maxMs": $MAX_MS,
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF

echo "cold-start: ${ELAPSED}ms (max ${MAX_MS}ms)"

if [ "$ELAPSED" -gt "$MAX_MS" ]; then
  echo "perf-gate-tier-c: FAIL" >&2
  exit 1
fi

echo ""
echo "perf-gate-tier-c: PASS"

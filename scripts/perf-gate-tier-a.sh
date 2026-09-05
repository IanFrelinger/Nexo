#!/usr/bin/env bash
# Perf Tier A: micro-benchmark / performance-scoped unit tests.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".ashlar/perf"
mkdir -p "$REPORT_DIR"

ORCH="src/Ashlar.Tests.Orchestration/Ashlar.Tests.Orchestration.csproj"
BG="src/Ashlar.Tests.BackgroundAgents/Ashlar.Tests.BackgroundAgents.csproj"

echo "== Perf Tier A: orchestration performance tests (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$ORCH" \
  --expected-prefix "Ashlar.Tests.Orchestration." \
  --min-tests 3 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~Ashlar.Tests.Orchestration.Performance" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo "== Perf Tier A: background agent performance tests (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$BG" \
  --expected-prefix "Ashlar.Tests.BackgroundAgents." \
  --min-tests 9 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~Ashlar.Tests.BackgroundAgents.Performance" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

printf '%s\n' '{"counted":true,"orchestration":3,"backgroundAgents":9}' \
  >"$REPORT_DIR/counted-summary.json"

echo ""
echo "perf-gate-tier-a: PASS"

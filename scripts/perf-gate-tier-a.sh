#!/usr/bin/env bash
# Perf Tier A: micro-benchmark / performance-scoped unit tests.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".nexo/perf"
mkdir -p "$REPORT_DIR"

ORCH="src/Nexo.Tests.Orchestration/Nexo.Tests.Orchestration.csproj"
BG="src/Nexo.Tests.BackgroundAgents/Nexo.Tests.BackgroundAgents.csproj"

echo "== Perf Tier A: orchestration performance tests =="
dotnet build "$ORCH" -v minimal
dotnet test "$ORCH" -f net8.0 --no-build \
  --filter "FullyQualifiedName~Nexo.Tests.Orchestration.Performance" \
  --logger "trx;LogFileName=perf-orchestration.trx" \
  --results-directory "$REPORT_DIR" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo "== Perf Tier A: background agent performance tests =="
dotnet build "$BG" -v minimal
dotnet test "$BG" -f net8.0 --no-build \
  --filter "FullyQualifiedName~Nexo.Tests.BackgroundAgents.Performance" \
  --logger "trx;LogFileName=perf-background-agents.trx" \
  --results-directory "$REPORT_DIR" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo ""
echo "perf-gate-tier-a: PASS"

#!/usr/bin/env bash
# DR Tier B: user knowledge log store durability tests.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== DR Tier B: LiteDB user knowledge log store (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 8 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~LiteDbUserKnowledgeLogStoreTests" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

REPORT_DIR=".ashlar/dr-gate"
mkdir -p "$REPORT_DIR"
echo '{"ok": true, "component": "user-knowledge-log", "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}' \
  >"$REPORT_DIR/knowledge-store.json"

echo ""
echo "dr-gate-tier-b: PASS"

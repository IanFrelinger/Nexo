#!/usr/bin/env bash
# Compat Tier C: configuration binding + doctor smoke.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
CLI="application/src/Ashlar.CLI/Ashlar.CLI.csproj"

echo "== Compat Tier C: configuration override tests (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 2 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~PipelineServiceCollectionExtensionsTests.AddPipelineCompositionLayer_WithConfiguration" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo "== Compat Tier C: hosting profile resolution (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 4 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~KernelPhaseResolutionTests" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo "== Compat Tier C: doctor smoke =="
dotnet build "$CLI" -v minimal >/dev/null
OUT=$(dotnet run --project "$CLI" --no-build -- doctor --json 2>&1)
echo "$OUT" | python3 -c '
import json, sys
text = sys.stdin.read()
start = text.find("{")
if start < 0:
    if "overall: PASS" in text:
        print("doctor smoke: PASS (text report)")
        raise SystemExit(0)
    raise SystemExit("doctor: no JSON output")
doc = json.loads(text[start:])
if not doc.get("ok", False):
    raise SystemExit(f"doctor failed: {doc}")
print("doctor smoke: PASS")
'

REPORT_DIR=".ashlar/compat"
mkdir -p "$REPORT_DIR"
echo "$OUT" >"$REPORT_DIR/doctor.json"

echo ""
echo "compat-gate-tier-c: PASS"

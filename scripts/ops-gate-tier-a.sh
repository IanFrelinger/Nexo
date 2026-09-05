#!/usr/bin/env bash
# Ops Gate Tier A — dogfood Blocks 1–6 (observe → analyze → adapt → promote → autonomy → self-context).
#
# Makefile dogfood-block1 through dogfood-block6 remain developer shortcuts. This
# gate does not invoke them. A raw project-wide filter can pass while a listed
# block class is missing, so the counted wrapper is the only path here.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="$ROOT/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
MIN_TESTS="${OPS_GATE_MIN_DOGFOOD_TESTS:-6}"
FILTER="FullyQualifiedName~DogfoodBlock1Tests|\
FullyQualifiedName~DogfoodBlock2Tests|\
FullyQualifiedName~DogfoodBlock3Tests|\
FullyQualifiedName~DogfoodBlock4Tests|\
FullyQualifiedName~DogfoodBlock5Tests|\
FullyQualifiedName~DogfoodBlock6Tests"

echo "==> ops-gate-tier-a: counted dogfood Blocks 1–6 (min ${MIN_TESTS})"
python3 "$ROOT/scripts/run-dotnet-test-counted.py" \
  --project "$INFRA" \
  --filter "$FILTER" \
  --min-tests "$MIN_TESTS" \
  -- \
  -c Release \
  -f net8.0 \
  --verbosity minimal

echo ""
echo "ops-gate-tier-a: PASS (verified: counted-dogfood-1-6)"

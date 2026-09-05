#!/usr/bin/env bash
# Ops Gate Tier B — dogfood Blocks 7–9 plus local IPC mesh.
#
# Makefile per-block dogfood targets are developer shortcuts that now call
# scripts/run-dogfood-block.sh (same counted wrapper). This gate does not
# invoke them.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="$ROOT/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
MIN_TESTS="${OPS_GATE_MIN_DOGFOOD_B_TESTS:-4}"
FILTER="FullyQualifiedName~DogfoodBlock7Tests|\
FullyQualifiedName~DogfoodBlock8Tests|\
FullyQualifiedName~DogfoodBlock9Tests|\
FullyQualifiedName~DogfoodBlock9LocalIpcTests"

echo "==> ops-gate-tier-b: counted dogfood Blocks 7–9 + IPC (min ${MIN_TESTS})"
python3 "$ROOT/scripts/run-dotnet-test-counted.py" \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodBlock" \
  --min-tests "$MIN_TESTS" \
  -- \
  -c Release \
  -f net8.0 \
  --filter "$FILTER" \
  --verbosity minimal

echo ""
echo "ops-gate-tier-b: PASS (verified: counted-dogfood-7-9-ipc)"

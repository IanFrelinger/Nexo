#!/usr/bin/env bash
# gRPC transport ProdStyle (Kestrel integration). A raw Category=ProdStyle
# filter still exits 0 when discovery matches nothing.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export ASHLAR_ALLOW_MOCK="${ASHLAR_ALLOW_MOCK:-1}"

echo "== gRPC transport: ProdStyle (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project src/Ashlar.Tests.Transport/Ashlar.Tests.Transport.csproj \
  --expected-prefix "Ashlar.Tests.Transport." \
  --min-tests 81 \
  -- \
  -f net8.0 \
  --filter "Category=ProdStyle" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo ""
echo "grpc-transport-gate: PASS"

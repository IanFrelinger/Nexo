#!/usr/bin/env bash
# Distribution-matrix IAshlarClient VirtualProduction slice (net10.0, counted).
# A renamed or deleted demo test used to make `dotnet test --filter` exit 0.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== distribution-matrix: IAshlarClient VirtualProduction (net10.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 1 \
  -- \
  -f net10.0 \
  --filter "FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.VirtualProduction.FrameworkVirtualProdDemosTests.Virtual_prod_IAshlarClient_GetStatusAsync_matches_console_and_blazor_demos" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo ""
echo "distribution-matrix-iashlar-client: PASS"

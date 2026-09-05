#!/usr/bin/env bash
# Application Tier C: in-process Ashlar.API (WebApplicationFactory) HTTP contract tests.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Application Tier C: in-process API (net10.0, counted) =="
# Ashlar.API and Tests/API + VirtualProduction demos compile on net10.0 only.
# The previous net8 filter matched ZERO cases and still exited 0.
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 4 \
  -- \
  -f net10.0 \
  --filter "FullyQualifiedName~ApiDevelopmentHostDiTests|FullyQualifiedName~FrameworkVirtualProdDemosTests" \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "application-gate-tier-c: PASS"

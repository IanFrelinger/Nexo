#!/usr/bin/env bash
# Security Tier B: API auth + mesh security middleware + open-internet readiness.
# Ashlar.API and Tests/API/** ship on net10.0 only. The net8 TFM excludes those
# files, so a net8 test filter matches zero API cases and still exits 0. The
# counted wrapper on net10.0 is the fail-closed runner.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Security Tier B: API security middleware (net10.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 44 \
  -- \
  -f net10.0 \
  --filter "FullyQualifiedName~AshlarApiKeyAuthMiddlewareTests|FullyQualifiedName~MeshSecurityMiddlewareTests|FullyQualifiedName~AshlarApiOpenInternetReadinessTests|FullyQualifiedName~SecurityAdvisoryEndpointTests|FullyQualifiedName~SecurityAnalysisRuleTests" \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "security-gate-tier-b: PASS"

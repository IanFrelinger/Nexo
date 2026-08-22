#!/usr/bin/env bash
# Security Tier B: API auth + mesh security middleware + open-internet readiness.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Security Tier B: API security middleware =="
dotnet build "$INFRA" -f net8.0 -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~AshlarApiKeyAuthMiddlewareTests|FullyQualifiedName~MeshSecurityMiddlewareTests|FullyQualifiedName~AshlarApiOpenInternetReadinessTests|FullyQualifiedName~SecurityAdvisoryEndpointTests|FullyQualifiedName~SecurityAnalysisRuleTests" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "security-gate-tier-b: PASS"

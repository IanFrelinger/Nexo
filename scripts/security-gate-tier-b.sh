#!/usr/bin/env bash
# Security Tier B: API auth + mesh security middleware + open-internet readiness.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"

echo "== Security Tier B: API security middleware =="
dotnet build "$INFRA" -f net8.0 -v minimal
NEXO_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~NexoApiKeyAuthMiddlewareTests|FullyQualifiedName~MeshSecurityMiddlewareTests|FullyQualifiedName~NexoApiOpenInternetReadinessTests|FullyQualifiedName~SecurityAdvisoryEndpointTests|FullyQualifiedName~SecurityAnalysisRuleTests" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "security-gate-tier-b: PASS"

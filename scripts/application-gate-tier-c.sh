#!/usr/bin/env bash
# Application Tier C: in-process Nexo.API (WebApplicationFactory) HTTP contract tests.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"

echo "== Application Tier C: in-process API (DI + HTTP demos) =="
dotnet build "$INFRA" -f net8.0 -v minimal
NEXO_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~ApiDevelopmentHostDiTests|FullyQualifiedName~FrameworkVirtualProdDemosTests" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "application-gate-tier-c: PASS"

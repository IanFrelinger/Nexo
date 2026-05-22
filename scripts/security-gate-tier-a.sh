#!/usr/bin/env bash
# Security Tier A: trust core (policy packs, peer trust, audit log, access boundary).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"

echo "== Security Tier A: trust core (policy packs + peer trust + audit) =="
dotnet build "$INFRA" -f net8.0 -v minimal
NEXO_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~Nexo.Tests.Infrastructure.Tests.Trust|FullyQualifiedName~NexoPeerBrickExecutorTrustTests" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "security-gate-tier-a: PASS"

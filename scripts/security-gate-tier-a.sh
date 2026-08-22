#!/usr/bin/env bash
# Security Tier A: trust core (policy packs, peer trust, audit log, access boundary).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Security Tier A: trust core (policy packs + peer trust + audit) =="
dotnet build "$INFRA" -f net8.0 -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Trust|FullyQualifiedName~AshlarPeerBrickExecutorTrustTests" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "security-gate-tier-a: PASS"

#!/usr/bin/env bash
# Security Tier A: trust core (policy packs, peer trust, audit log, access boundary).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Security Tier A: trust core (net8.0, counted) =="
# Trust namespace + peer-executor cases. A rename that matches zero still
# exited 0 before the counted wrapper. Listed 97 identities.
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 97 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Trust|FullyQualifiedName~AshlarPeerBrickExecutorTrustTests" \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "security-gate-tier-a: PASS"

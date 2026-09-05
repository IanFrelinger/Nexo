#!/usr/bin/env bash
# Composition + mesh Tier A: pipeline composition (validate, decompose, schedule, orchestrate, lifecycle).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Composition Tier A: pipeline composition tests (net8.0, counted) =="
# Namespace filter. A rename that matches zero still exited 0 before the
# counted wrapper. Listed 64 identities with prefix Ashlar.Tests.Infrastructure.
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 64 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Pipelines" \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "composition-mesh-gate-tier-a: PASS"

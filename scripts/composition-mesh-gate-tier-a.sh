#!/usr/bin/env bash
# Composition + mesh Tier A: pipeline composition (validate, decompose, schedule, orchestrate, lifecycle).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Composition Tier A: pipeline composition tests =="
dotnet build "$INFRA" -f net8.0 -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Pipelines" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "composition-mesh-gate-tier-a: PASS"

#!/usr/bin/env bash
# Composition + mesh Tier C: in-process mesh fleet (task registry, placement, execution, elastic, persistence).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"

echo "== Mesh Tier C: fleet / clustered task control plane (in-process) =="
dotnet build "$INFRA" -f net8.0 -v minimal
NEXO_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~Nexo.Tests.Infrastructure.Tests.Fleet" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "composition-mesh-gate-tier-c: PASS"

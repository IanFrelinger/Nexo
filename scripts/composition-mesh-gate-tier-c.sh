#!/usr/bin/env bash
# Composition + mesh Tier C: in-process mesh fleet (task registry, placement, execution, elastic, persistence).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

FLEET_TESTS="commercial/tests/Ashlar.Commercial.Tests.Fleet/Ashlar.Commercial.Tests.Fleet.csproj"

echo "== Mesh Tier C: fleet / clustered task control plane (in-process) =="
dotnet build "$FLEET_TESTS" -f net8.0 -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$FLEET_TESTS" -f net8.0 --no-build \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "composition-mesh-gate-tier-c: PASS"

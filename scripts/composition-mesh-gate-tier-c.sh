#!/usr/bin/env bash
# Composition + mesh Tier C: in-process mesh fleet (task registry, placement, execution, elastic, persistence),
# the commercial Fleet.Host endpoint smoke suite, and MeshDirector CLI URI facts.
# Host tests are net10.0-only (WebApplicationFactory / TestHost 10). A net8 filter
# would match zero cases and still exit 0 — the counted wrapper is the fail-closed runner.
# MeshDirector is net8.0-only; the counted wrapper is still required so a silent
# empty match cannot pass.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

FLEET_TESTS="commercial/tests/Ashlar.Commercial.Tests.Fleet/Ashlar.Commercial.Tests.Fleet.csproj"
FLEET_HOST_TESTS="commercial/tests/Ashlar.Commercial.Tests.Fleet.Host/Ashlar.Commercial.Tests.Fleet.Host.csproj"
MESH_DIRECTOR_TESTS="commercial/tests/Ashlar.Commercial.Tests.MeshDirector/Ashlar.Commercial.Tests.MeshDirector.csproj"

echo "== Mesh Tier C: fleet / clustered task control plane (in-process) =="
dotnet build "$FLEET_TESTS" -f net8.0 -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$FLEET_TESTS" -f net8.0 --no-build \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "== Mesh Tier C: commercial Fleet.Host endpoints (net10.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$FLEET_HOST_TESTS" \
  --expected-prefix "Ashlar.Commercial.Tests.Fleet.Host." \
  --min-tests 4 \
  -- \
  -c Release \
  -f net10.0 \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "== Mesh Tier C: commercial MeshDirector CLI URI (net8.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$MESH_DIRECTOR_TESTS" \
  --expected-prefix "Ashlar.Commercial.Tests.MeshDirector." \
  --min-tests 4 \
  -- \
  -c Release \
  -f net8.0 \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "composition-mesh-gate-tier-c: PASS"

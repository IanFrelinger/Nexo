#!/usr/bin/env bash
# Composition + mesh Tier B: CLI pipeline + open mesh command surfaces.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj"

echo "== Composition Tier B: open mesh CLI unit suites =="
dotnet build "$CLI" -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$CLI" -f net10.0 --no-build \
  --filter "FullyQualifiedName~UnitTestBridgeTests&(DisplayName~PipelineCommand|DisplayName~MeshCommand|DisplayName~OptimizeAgentCluster)" \
  --blame-hang-timeout 180s --blame-hang-dump-type none

echo ""
echo "composition-mesh-gate-tier-b: PASS"

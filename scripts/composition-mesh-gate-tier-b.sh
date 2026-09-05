#!/usr/bin/env bash
# Composition + mesh Tier B: CLI pipeline + open mesh command surfaces.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj"

echo "== Composition Tier B: open mesh CLI unit suites (net10.0, counted) =="
# Three named UnitTestBridgeTests rows. A DisplayName rename that matches
# zero still exited 0 before the counted wrapper.
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$CLI" \
  --expected-prefix "Ashlar.Tests.CLI." \
  --min-tests 3 \
  -- \
  -f net10.0 \
  --filter "FullyQualifiedName~UnitTestBridgeTests&(DisplayName~PipelineCommand|DisplayName~MeshCommand|DisplayName~OptimizeAgentCluster)" \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "composition-mesh-gate-tier-b: PASS"

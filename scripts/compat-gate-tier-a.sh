#!/usr/bin/env bash
# Compat Tier A: schema / migration / composition compatibility tests.
# Mesh checkpoint tests live in Ashlar.Commercial.Tests.Fleet. The previous
# Infrastructure filter matched zero identities and still exited 0.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
FLEET_TESTS="commercial/tests/Ashlar.Commercial.Tests.Fleet/Ashlar.Commercial.Tests.Fleet.csproj"

echo "== Compat Tier A: mesh checkpoint migration (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$FLEET_TESTS" \
  --expected-prefix "Ashlar.Commercial.Tests.Fleet." \
  --min-tests 1 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~MeshTaskExecutionServiceTests.MigrateForCheckpointAsync" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo "== Compat Tier A: LiteDB persistence registration (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 1 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~PipelineServiceCollectionExtensionsTests.AddPipelineCompositionLayer_WithLiteDbPersistence" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo "== Compat Tier A: composition registry validation (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 4 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~CompositionRegistryValidationTests" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo ""
echo "compat-gate-tier-a: PASS"

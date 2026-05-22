#!/usr/bin/env bash
# Compat Tier A: schema / migration / composition compatibility tests.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"

echo "== Compat Tier A: mesh checkpoint migration =="
dotnet build "$INFRA" -v minimal
dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~MeshTaskExecutionServiceTests.MigrateForCheckpointAsync" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo "== Compat Tier A: LiteDB persistence registration =="
dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~PipelineServiceCollectionExtensionsTests.AddPipelineCompositionLayer_WithLiteDbPersistence" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo "== Compat Tier A: composition registry validation =="
dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~CompositionRegistryValidationTests" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo ""
echo "compat-gate-tier-a: PASS"

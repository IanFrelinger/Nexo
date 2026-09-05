#!/usr/bin/env bash
# Counted test slices for Production Readiness Gate v1.
# A raw filter still exits 0 when discovery matches nothing.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if command -v python3 >/dev/null 2>&1; then
  PY=python3
else
  PY=python
fi

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
export ASHLAR_ALLOW_MOCK="${ASHLAR_ALLOW_MOCK:-1}"

echo "== Production readiness: Pipelines (net8.0, counted) =="
"$PY" scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 68 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~Pipelines" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo ""
echo "== Production readiness: Pipelines (net10.0, counted) =="
"$PY" scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 68 \
  -- \
  -f net10.0 \
  --filter "FullyQualifiedName~Pipelines" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo ""
echo "== Production readiness: host DI smoke (net8.0, counted) =="
"$PY" scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 2 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~HostingE2ESmokeTests.AddAshlar_RegistersObservationPipeline_ByDefault|FullyQualifiedName~Pipelines.PipelineServiceCollectionExtensionsTests.AddAshlar_RegistersPipelineCompositionLayerByDefault" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo ""
echo "production-readiness-gate-v1-tests: PASS"

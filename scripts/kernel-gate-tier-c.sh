#!/usr/bin/env bash
# Tier C kernel gate: extended ProdStyle (Infrastructure), workflow executor, optional mesh lab.
# See docs/production-readiness/KernelHardeningPlan-v1.md
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Tier C: ProdStyle Infrastructure (net8, FluentAssertions-safe filter) =="
make test-prod-style

echo "== Tier C: workflow executor integration (net8.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 12 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~WorkflowExecutorIntegrationTests" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo "== Tier C: gRPC transport ProdStyle (counted) =="
bash scripts/grpc-transport-gate.sh

echo "== Tier C: air-gapped profile smoke (net10.0, counted) =="
# net8.0 omits AirGappedProfileApiHostProdStyleTests (API host is net10.0 only).
# EnrolledSuiteConventionTests method names contain AirGapped and used to
# inflate this slice (19 listed / floor 18). Product identities are 17.
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 17 \
  -- \
  -f net10.0 \
  --filter "FullyQualifiedName~AirGapped&FullyQualifiedName!~EnrolledSuiteConventionTests" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

if [ "${KERNEL_GATE_MESH_E2E:-0}" = "1" ] && [ -f ".env.mesh-lab" ]; then
  echo "== Tier C: mesh virtual lab E2E (compose up + verify + down) =="
  bash scripts/run-mesh-lab-e2e.sh .env.mesh-lab
elif [ -f ".env.mesh-lab" ]; then
  echo "== Tier C: mesh lab skipped (set KERNEL_GATE_MESH_E2E=1 for full E2E; or run: make mesh-lab-e2e) =="
else
  echo "== Tier C: mesh lab skipped (run: make bootstrap-mesh-lab-env) =="
fi

echo ""
echo "kernel-gate-tier-c: PASS"

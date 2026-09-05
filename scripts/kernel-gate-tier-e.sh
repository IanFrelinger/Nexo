#!/usr/bin/env bash
# Tier E kernel gate: observability, prod-shaped Compose dry run, performance-scoped tests.
# Advisory for release; required before calling kernel "production-operable" on Linux hosts with Docker.
# See docs/production-readiness/KernelHardeningPlan-v1.md and docs/prod-dry-run.md
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
ORCH="src/Ashlar.Tests.Orchestration/Ashlar.Tests.Orchestration.csproj"

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "error: kernel-gate-tier-e requires a working Docker daemon; refusing to skip prod-dry-run" >&2
  exit 2
fi

echo "== Tier E: OpenTelemetry registration (net8.0, counted) =="
dotnet build "$INFRA" -v minimal
# Counted wrapper: an empty OpenTelemetryTests filter used to exit 0.
python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 1 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~OpenTelemetryTests" \
  --blame-hang-timeout 60s --blame-hang-dump-type none

echo "== Tier E: orchestration performance-scoped tests (net8.0, counted) =="
dotnet build "$ORCH" -v minimal
# Counted wrapper: an empty Performance namespace filter used to exit 0.
python3 scripts/run-dotnet-test-counted.py \
  --project "$ORCH" \
  --expected-prefix "Ashlar.Tests.Orchestration." \
  --min-tests 3 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~Ashlar.Tests.Orchestration.Performance" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo "== Tier E: production-shaped Compose dry run (portal) =="
# grpc.tools protoc can segfault on linux_arm64 during API image build; match mesh-lab CI default.
export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"
bash scripts/prod-dry-run.sh --portal

if [ -f ".env.mesh-lab" ] && [ "${KERNEL_GATE_CHAOS_LITE:-0}" = "1" ]; then
  echo "== Tier E: chaos-lite (mesh network-negative; stack must be up) =="
  make mesh-lab-up 2>/dev/null || true
  bash scripts/mesh-lab-verify-network-negative.sh .env.mesh-lab || {
    echo "chaos-lite: mesh-lab-verify-network-negative failed (is mesh lab up? run: make mesh-lab-e2e)" >&2
    exit 1
  }
else
  echo "== Tier E: chaos-lite skipped (KERNEL_GATE_CHAOS_LITE=1 + .env.mesh-lab to enable) =="
fi

echo ""
echo "kernel-gate-tier-e: PASS"
echo "Quarterly: complete docs/production-readiness/KernelChaosDrill-v1.md and record RPO/RTO."

#!/usr/bin/env bash
# Kernel Tier A test slices: hosting profile matrix + pipeline lifecycle.
# make kernel-gate builds the runtime graph first, then calls this script.
# Counted wrappers refuse a silent empty filter (dotnet test exits 0 on zero matches).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Kernel Tier A: hosting profile / E2E smoke (net8.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 40 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~KernelPhaseResolutionTests|FullyQualifiedName~HostingDeploymentProfileTests|FullyQualifiedName~HostingE2ESmokeTests" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo "== Kernel Tier A: pipeline validator / lifecycle (net8.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 14 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~PipelineTemplateValidatorTests|FullyQualifiedName~PipelineLifecycleE2ETests" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

echo ""
echo "kernel-gate-tier-a: PASS"

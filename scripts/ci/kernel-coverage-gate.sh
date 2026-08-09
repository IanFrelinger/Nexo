#!/usr/bin/env bash
# Enforces line-coverage floors for kernel assemblies (matches .github/workflows/kernel-coverage-gate.yml).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
mkdir -p CoverageReports

echo "== Domain (Nexo.Core.Domain) line coverage: 100% required =="
dotnet test src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$ROOT/CoverageReports/domain" \
  /p:CoverletOutputFormat=cobertura \
  /p:Include="[Nexo.Core.Domain]*" \
  /p:Threshold="${DOMAIN_COVERAGE_THRESHOLD:-100}" \
  /p:ThresholdType=line \
  --verbosity minimal


# == Infrastructure (Nexo.Infrastructure): EXCLUDED ==
#
# Still excluded, but NOT for the reason this comment used to give. The
# LoaderAllocatorScout / 0x80131506 crash that made this step impossible is FIXED
# (see BrickMutationEngine: overlapping collectible AssemblyLoadContexts in the
# certification mutation engine).
#
# Fixing it revealed a SECOND defect, now also FIXED: a dependency CYCLE that hung
# API host startup. IBackgroundAgentRegistry -> ISelfExtendRunner ->
# SelfExtendRunnerAdapter -> IBackgroundAgentRegistry, laundered through factory
# lambdas so Microsoft.Extensions.DependencyInjection could not detect it — the graph
# passed ValidateOnBuild and then recursed at resolution time. Broken by deferring the
# registry behind Lazy<T>.
#
# And fixing THAT revealed a THIRD, which is why this step is still excluded: a
# process-lifetime leak. InfrastructureRoutingGapCoverageTests passes 7/7 in under a
# second and then the test host NEVER EXITS. coverlet.msbuild writes its report only
# after the host exits, so the step waits forever and the job is killed by its cap —
# with no crash, no failing test, and no output. Three capped runs died this way:
#
#   30-minute cap -> cancelled at 30m20s, 0 crash signatures
#   60-minute cap -> cancelled at 60m20s, 0 crash signatures
#   45-minute cap -> cancelled at 45m21s, 0 crash signatures
#
# Raising the cap cannot help: the process never exits, so no cap is large enough.
#
# Each defect was invisible until the one in front of it was fixed. Domain and
# Core.Application below both complete normally, so the gate returns a real pass/fail
# rather than burning the runner.
#
# NOTE ON THE OLD 83% FLOOR: it was never measured against a complete run. Every
# historical run was truncated by the crash at a different point, so 83 is not a
# figure to restore — the real floor should come from the first run that finishes.
#
# Tracked: docs/production-readiness/KernelCoverageGate-Findings.md
# Related: TestRunnerAdapter.ExecuteTestAsync abandons its runTask on the timeout
# path (latent, separate); the test-host process-lifetime leak (open, blocks this step).
echo ""
echo "== Core.Application line coverage: ${APP_COVERAGE_THRESHOLD:-67}% floor =="
dotnet test src/Nexo.Tests.Application/Nexo.Tests.Application.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$ROOT/CoverageReports/app" \
  /p:CoverletOutputFormat=cobertura \
  /p:Include="[Nexo.Core.Application]*" \
  /p:Threshold="${APP_COVERAGE_THRESHOLD:-67}" \
  /p:ThresholdType=line \
  --verbosity minimal

echo ""
echo "kernel-coverage-gate: PASS"

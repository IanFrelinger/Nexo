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


# == Infrastructure (Nexo.Infrastructure) ==
#
# RESTORED. Four defects blocked this step, each hidden by the one before it, all now
# fixed:
#
#   1. Collectible-AssemblyLoadContext crash in the certification mutation engine.
#   2. A DI cycle that hung API host startup (registry -> self-extend -> registry).
#   3. AddNexoFederatedBrickMesh recursing into its own registration, so a test never
#      completed and the test host never exited -- which is what actually starved this
#      step, since coverlet writes its report only after the host exits.
#   4. ProviderFactory doing blocking network I/O in its constructor.
#
# THE FLOOR BELOW IS PROVISIONAL (0), for one measurement run. No trustworthy figure
# has ever existed: every historical run was truncated, so the old 83% was never
# measured against a complete run. A floor of 0 keeps "completes but low" (reported)
# distinguishable from "crashes or hangs" (fails). The real floor follows once this
# reports.
#
# Tracked: docs/production-readiness/KernelCoverageGate-Findings.md
# Related: TestRunnerAdapter.ExecuteTestAsync abandons its runTask on the timeout path
# (latent, separate); OllamaProvider still blocks in its constructor (separate).
echo ""
echo "== Infrastructure (Nexo.Infrastructure) line coverage: PROVISIONAL floor ${INFRA_COVERAGE_THRESHOLD:-0}% (measuring the real number) =="
# Daemon black-box tests excluded from the COVERAGE run only: ~7.5 min of spawned-
# process timeouts whose work cannot be attributed to [Nexo.Infrastructure] anyway.
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net9.0 \
  --filter "FullyQualifiedName!~RuntimeStudioBlackBoxSmokeTests" \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$ROOT/CoverageReports/infra" \
  /p:CoverletOutputFormat=cobertura \
  /p:Include="[Nexo.Infrastructure]*" \
  /p:Threshold="${INFRA_COVERAGE_THRESHOLD:-0}" \
  /p:ThresholdType=line \
  --verbosity minimal
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

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
# Not a coverage decision — the step cannot produce a number at all. The
# Nexo.Tests.Infrastructure host crashes during TEARDOWN
# (LoaderAllocatorScout.Finalize / 0x80131506) after every test has passed, and the
# resulting aborted run destroys the coverage results before they are written. With
# no result the step never returns, which is why this job ran to GitHub's 6-hour cap
# and was auto-cancelled on master, on dependabot branches, and everywhere else. It
# has never once produced a verdict.
#
# Both instrumentation routes were tried and BOTH fail the same way:
#   * coverlet.msbuild  -> 572/572 pass, host crashes, no report
#   * XPlat Code Coverage data collector (coverlet.collector) -> 452/453 pass,
#     host crashes, no report. Collecting out-of-process does not help because the
#     run is ABORTED, so attachments are discarded too.
#
# So Infrastructure is excluded until the teardown crash itself is fixed. Domain and
# Core.Application below both complete normally, so the gate now returns a real
# pass/fail instead of hanging.
#
# Tracked: docs/production-readiness/KernelCoverageGate-Findings.md
# Related: TestRunnerAdapter.ExecuteTestAsync abandons its runTask on the timeout
# path (latent, separate); Nexo.Tests.Infrastructure teardown crash (this).
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

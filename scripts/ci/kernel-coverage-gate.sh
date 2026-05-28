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

echo ""
echo "== Infrastructure (Nexo.Infrastructure) line coverage: ${INFRA_COVERAGE_THRESHOLD:-83}% floor =="
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net9.0 \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$ROOT/CoverageReports/infra" \
  /p:CoverletOutputFormat=cobertura \
  /p:Include="[Nexo.Infrastructure]*" \
  /p:Threshold="${INFRA_COVERAGE_THRESHOLD:-83}" \
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

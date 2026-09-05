#!/usr/bin/env bash
# Local reproduction of the cert-gate CI job (hermetic certification + generation tests).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${ROOT}"

# shellcheck source=scripts/cert-gate-config.sh
source "${ROOT}/scripts/cert-gate-config.sh"

FILTER="${CERT_GATE_FILTER}"
RESULTS_DIR="${ROOT}/test-results"
TRX="${RESULTS_DIR}/cert-gate.trx"

# Hermetic tests: do not use portability NuGet config (forces Roslyn path in GeneratedBrickBuilder).
unset ASHLAR_CERT_NUGET_CONFIG

mkdir -p "${RESULTS_DIR}"

echo "== cert-gate: restore =="
dotnet restore src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj
dotnet restore src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj

echo "== cert-gate: build =="
dotnet build src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj -f net8.0 --no-restore -v minimal

echo "== cert-gate: test =="
set +e
dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj \
  -f net8.0 \
  --no-build \
  --filter "${FILTER}" \
  --logger "trx;LogFileName=cert-gate.trx" \
  --logger "console;verbosity=normal" \
  --results-directory "${RESULTS_DIR}"
TEST_EXIT=$?
set -e

bash scripts/cert-gate-zero-test-guard.sh "${TRX}"

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  bash scripts/cert-gate-summary.sh "${TRX}"
fi

if [[ "${TEST_EXIT}" -ne 0 ]]; then
  exit "${TEST_EXIT}"
fi

# Convention facts used to ride the Certification substring and inflate the live
# total. They still run on the required check, with a static unique floor.
echo "== cert-gate: enrolled suite conventions (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj \
  --expected-prefix "Ashlar.Tests.Infrastructure.Tests.Certification.EnrolledSuiteConventionTests." \
  --min-tests 117 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~EnrolledSuiteConventionTests" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

# Analyzer unit triads used to be sln-only / UNOWNED. They are the fence catalog
# the required PR check exists to protect; the counted wrapper is fail-closed.
echo "== cert-gate: analyzer unit suite (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project src/Ashlar.Analyzers.Tests/Ashlar.Analyzers.Tests.csproj \
  --expected-prefix "Ashlar.Analyzers.Tests." \
  --min-tests 56 \
  -- \
  -c Release \
  -f net8.0 \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

# HTTP + distributed contract records used to ride products-gate's 5-test
# Distributed subset only. The tests lane requires 18 unique identities;
# cert-gate is the required PR check, so the same floor runs here.
echo "== cert-gate: contracts suite (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project src/Ashlar.Tests.Contracts/Ashlar.Tests.Contracts.csproj \
  --expected-prefix "Ashlar.Tests.Contracts." \
  --min-tests 18 \
  -- \
  -c Release \
  -f net8.0 \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

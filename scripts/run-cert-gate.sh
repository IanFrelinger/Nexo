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
unset NEXO_CERT_NUGET_CONFIG

mkdir -p "${RESULTS_DIR}"

echo "== cert-gate: restore =="
dotnet restore src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj
dotnet restore src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj

echo "== cert-gate: build =="
dotnet build src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 --no-restore -v minimal

echo "== cert-gate: test =="
set +e
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
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

exit "${TEST_EXIT}"

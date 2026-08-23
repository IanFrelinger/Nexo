#!/usr/bin/env bash
# Single source of truth for cert-gate test filter and expected count.
set -euo pipefail

# Must match tests exercised by scripts/run-cert-gate.sh and .github/workflows/cert-gate.yml
readonly CERT_GATE_FILTER='FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Certification|FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Adaptation.GenerationSafety|FullyQualifiedName~AstMutationEngineTests'

# The expected test count is derived at RUNTIME from `dotnet test --list-tests` (see
# cert_gate_expected_count below); there is no hardcoded total to keep in sync. A previous
# per-class enumeration here summed to 99 while the gate actually ran 178 — it had drifted
# by 79 and read as authoritative, so it was removed rather than re-pinned. Do not re-add a
# static count: the zero-test guard fails loudly if discovery ever returns nothing.
#
# Excluded from cert-gate filter: LocalFixtures.CompositionAcceptanceRateBatchFixtureGeneratorTests (local fixture regen only)

cert_gate_list_tests() {
  local root="${1:?repo root required}"
  dotnet test "${root}/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj" \
    -f net8.0 \
    --no-build \
    --list-tests \
    --filter "${CERT_GATE_FILTER}" 2>/dev/null \
    | grep -E '^[[:space:]]*Ashlar\.Tests\.' || true
}

cert_gate_expected_count() {
  local root="${1:?repo root required}"
  cert_gate_list_tests "${root}" | wc -l | tr -d '[:space:]'
}

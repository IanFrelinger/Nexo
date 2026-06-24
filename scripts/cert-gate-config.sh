#!/usr/bin/env bash
# Single source of truth for cert-gate test filter and expected count.
set -euo pipefail

# Must match tests exercised by scripts/run-cert-gate.sh and .github/workflows/cert-gate.yml
readonly CERT_GATE_FILTER='FullyQualifiedName~Nexo.Tests.Infrastructure.Tests.Certification|FullyQualifiedName~Nexo.Tests.Infrastructure.Tests.Adaptation.GenerationSafety|FullyQualifiedName~AstMutationEngineTests'

# Documented breakdown (must match --list-tests output; guard derives count at runtime):
#   CertificationGateTeethTests: 6
#   AstMutationEngineTests: 2
#   GenerationSafetyTests: 4
#   CompositionCertificationGateTeethTests: 5
#   DamageResolverDogfoodTests: 2
#   CompositionDogfoodTests: 2
#   CrossProjectReuseTests: 3
#   CompositionProposerDogfoodTests: 8
#   CompositionProposerIndependenceTests: 3
#   RealModelCompositionProposerDogfoodTests: 2
#   CompositionAcceptanceRateMeasurementTests: 3
#   CompositionAcceptanceRateProtocolTests: 1
# Excluded from cert-gate filter: LocalFixtures.CompositionAcceptanceRateBatchFixtureGeneratorTests (local fixture regen only)

cert_gate_list_tests() {
  local root="${1:?repo root required}"
  dotnet test "${root}/src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj" \
    -f net8.0 \
    --no-build \
    --list-tests \
    --filter "${CERT_GATE_FILTER}" 2>/dev/null \
    | grep -E '^[[:space:]]*Nexo\.Tests\.' || true
}

cert_gate_expected_count() {
  local root="${1:?repo root required}"
  cert_gate_list_tests "${root}" | wc -l | tr -d '[:space:]'
}

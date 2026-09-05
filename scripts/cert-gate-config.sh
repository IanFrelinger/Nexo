#!/usr/bin/env bash
# Single source of truth for cert-gate test filter and expected count.
set -euo pipefail

# Must match tests exercised by scripts/run-cert-gate.sh and .github/workflows/cert-gate.yml
# Product certification + generation-safety + mutation engine. EnrolledSuiteConventionTests
# live in Tests.Certification and used to inflate the live total; they run as their own
# counted slice in run-cert-gate.sh.
readonly CERT_GATE_FILTER='(FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Certification|FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Adaptation.GenerationSafety|FullyQualifiedName~AstMutationEngineTests)&FullyQualifiedName!~EnrolledSuiteConventionTests'

# The live expected count is derived at RUNTIME from `dotnet test --list-tests` (see
# cert_gate_expected_count below). Do not re-pin that live total: a previous per-class
# enumeration summed to 99 while the gate actually ran 178.
#
# CERT_GATE_MIN_TESTS is a collapse floor, not the live total. Product-suite listed
# count was 447 after excluding EnrolledSuiteConventionTests. Raise the floor when
# the suite earns it. Do not lower it to turn a red build green.
readonly CERT_GATE_MIN_TESTS=400
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

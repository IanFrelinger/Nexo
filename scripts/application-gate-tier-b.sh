#!/usr/bin/env bash
# Application Tier B: CLI unit tests (focused) + doctor report.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_TESTS="application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj"

echo "== Application Tier B: CLI command tests =="
dotnet build "$CLI_TESTS" -v minimal
# The whole CLI suite EXCEPT the xUnit bridge. Both halves of that sentence are load-bearing.
#
# The filter this replaces named ValidateCommandTests, TrustCommandTests and WorkflowCommandTests.
# Those types exist — but they are `UnitTestBase` suites, not xUnit ones, so VSTest saw no test by
# those names, matched ZERO, and exited 0: the lane was green because it ran nothing.
#
# Repairing the filter was only half of it, and the half that is easy to mistake for the whole. The
# STEP that invokes this script was `if: github.event_name == 'workflow_dispatch' && inputs.tier ==
# 'b'`, and this workflow's only automatic trigger is pull_request, so a correct filter behind a
# false condition is the same vacuous pass one level up. Both are fixed: the filter below matches
# the real suites, and application-gate.yml's Tier B `if:` is now the `!=` form Tier A uses, so this
# lane runs on every pull_request. Measured in the dev container at ~20s for 199 tests.
#
# UnitTestBridgeTests is the theory that DOES run the `UnitTestBase` suites, and it must stay
# excluded: `Framework_unit_test_passes(testType: AgentCommandTests)` never completes. Measured in
# the dev container — 22 of 34 rows finish, then --blame-hang aborts the run on inactivity, and
# ASHLAR_ALLOW_MOCK=1 does not change it. Without the exclusion this gate does not fail, it HANGS,
# which costs the whole job's wall clock and still reports nothing. Filed separately.
#
# Excluding the whole theory is coarser than it needs to be: a SINGLE row of it can be selected by
# narrowing on DisplayName, which is how scripts/composition-mesh-gate-tier-b.sh reaches
# PipelineCommand/MeshCommand/OptimizeAgentCluster and how scripts/security-gate-tier-c.sh reaches
# TrustCommandTests. So the `UnitTestBase` suites are not uncoverable — they are covered one named
# row at a time until the hanging AgentCommandTests row is fixed and the whole theory can run here.
ASHLAR_ALLOW_MOCK=1 dotnet test "$CLI_TESTS" -f net10.0 --no-build \
  --filter "FullyQualifiedName!~UnitTestBridgeTests" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo "== Application Tier B: doctor --json =="
set +e
ASHLAR_ALLOW_MOCK=1 dotnet run --project application/src/Ashlar.CLI/Ashlar.CLI.csproj -- doctor --json
doctor_exit=$?
set -e
if [ "$doctor_exit" -ne 0 ]; then
  echo "doctor --json exited $doctor_exit (warnings may fail strict profile; review output)" >&2
  if [ "${APPLICATION_GATE_STRICT_DOCTOR:-0}" = "1" ]; then
    exit "$doctor_exit"
  fi
fi

echo ""
echo "application-gate-tier-b: PASS"

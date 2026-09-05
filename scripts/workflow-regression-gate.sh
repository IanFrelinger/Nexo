#!/usr/bin/env bash
# Workflow regression: CLI WorkflowCommandTests plus baseline promote/report/gate.
# `ashlar test local` exits 0 on a silent empty match; refuse TotalTests < 1.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
CLI_TESTS="application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj"
OUT_DIR="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/workflow-regression-gate"
mkdir -p "$OUT_DIR" .ashlar/runtime .ashlar/workflow

echo "== Workflow regression: build CLI + tests =="
dotnet restore src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj
dotnet build "$CLI_TESTS" -v minimal

echo "== Workflow regression: WorkflowCommandTests (test local, fail-closed) =="
LOG="$OUT_DIR/test-local.json"
set +e
ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" -- test local --filter WorkflowCommandTests --format-json >"$LOG" 2>&1
code=$?
set -e
cat "$LOG"
if [ "$code" -ne 0 ]; then
  echo "error: ashlar test local exited $code" >&2
  echo "workflow-regression-gate: FAIL" >&2
  exit "$code"
fi
if ! python3 "$ROOT/scripts/lib/assert-test-local-floor.py" "$LOG"; then
  echo "workflow-regression-gate: FAIL" >&2
  exit 1
fi

echo "== Workflow regression: seed history and policy =="
cat > .ashlar/runtime/workflow_lab_history.jsonl <<'JSONL'
{"runId":"baseline-run","gitSha":"baseline123","specHash":"spec-baseline","providerSnapshot":"offline","scenarioId":"req::comp::profile::iter-1","requestId":"req","compositionId":"comp","modelProfileId":"profile","iteration":1,"startedAtUtc":"2026-01-01T00:00:00Z","elapsedMs":120,"success":true,"agentCount":1,"conflictCount":0,"escalationCount":0,"score":100.0,"summary":"baseline","skipped":false,"warmup":false,"failureCategory":"none","benchmarkSet":"workflow-lab"}
{"runId":"candidate-run","gitSha":"candidate123","specHash":"spec-candidate","providerSnapshot":"offline","scenarioId":"req::comp::profile::iter-1","requestId":"req","compositionId":"comp","modelProfileId":"profile","iteration":1,"startedAtUtc":"2026-01-02T00:00:00Z","elapsedMs":130,"success":true,"agentCount":1,"conflictCount":0,"escalationCount":0,"score":99.0,"summary":"candidate","skipped":false,"warmup":false,"failureCategory":"none","benchmarkSet":"workflow-lab"}
JSONL

cat > .ashlar/workflow/workflow_gate.policy.json <<'JSON'
{
  "benchmarkSet": "workflow-lab",
  "minSuccessRateDelta": -0.10,
  "maxP95LatencyRegressionMs": 300,
  "maxAverageLatencyRegressionMs": 200,
  "minAverageScoreDelta": -10.0,
  "maxRegressedScenarios": 2
}
JSON

echo "== Workflow regression: baseline promote =="
dotnet run --project "$CLI" -- workflow baseline promote \
  --repo-root . \
  --benchmark-set workflow-lab \
  --run-id baseline-run \
  --policy-file .ashlar/workflow/workflow_gate.policy.json \
  --json

echo "== Workflow regression: report compare =="
dotnet run --project "$CLI" -- workflow report \
  --repo-root . \
  --benchmark-set workflow-lab \
  --run-id candidate-run \
  --baseline-run-id baseline-run \
  --output "$OUT_DIR/workflow_report.md"

echo "== Workflow regression: gate with active baseline =="
dotnet run --project "$CLI" -- workflow gate \
  --repo-root . \
  --benchmark-set workflow-lab \
  --run-id candidate-run \
  --policy-file .ashlar/workflow/workflow_gate.policy.json

echo ""
echo "workflow-regression-gate: PASS"

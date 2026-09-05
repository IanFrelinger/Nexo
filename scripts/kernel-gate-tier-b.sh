#!/usr/bin/env bash
# Tier B kernel gate: production-readiness checks (build, CLI fail-closed, LiteDB resume).
# See docs/ProductionReadinessGate-v1.md and docs/production-readiness/KernelHardeningPlan-v1.md
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_PROJECT="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
HELPER="$ROOT/scripts/lib/assert-pipeline-fail-closed.py"
TMP_ROOT="${TMPDIR:-/tmp}/ashlar-kernel-gate-$$"
export TMP_ROOT
mkdir -p "$TMP_ROOT"
trap 'rm -rf "$TMP_ROOT"' EXIT

TEMPLATE_PATH="$TMP_ROOT/pipeline_gate_demo.json"
RESUME_DB="$TMP_ROOT/ashlar_pipeline_gate_resume.db"

run_expecting_failure() {
  local log="$1"
  shift
  set +e
  "$@" | tee "$log"
  local exit_code=${PIPESTATUS[0]}
  set -e
  if [ "$exit_code" -eq 0 ]; then
    echo "expected non-zero from: $*" >&2
    exit 1
  fi
}

echo "== Tier B: build checks =="
dotnet build src/Ashlar.Core.Application/Ashlar.Core.Application.csproj -f netstandard2.0 -v minimal
dotnet build src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj -v minimal
dotnet build "$CLI_PROJECT" -v minimal

echo "== Tier B: pipeline lifecycle tests (net8.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 14 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~PipelineTemplateValidatorTests|FullyQualifiedName~PipelineLifecycleE2ETests" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

cat > "$TEMPLATE_PATH" <<'JSON'
{
  "templateId": "gate-demo",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
JSON

echo "== Tier B: CLI validate / fail-closed run / diagnostics =="
VALIDATE_LOG="$TMP_ROOT/gate-validate.log"
UNCONFIGURED_LOG="$TMP_ROOT/gate-run-unconfigured.log"
HOOKS_LOG="$TMP_ROOT/gate-run-hooks.log"
DIAGNOSTICS_LOG="$TMP_ROOT/gate-diagnostics.log"

dotnet run --project "$CLI_PROJECT" -- pipeline validate --template "$TEMPLATE_PATH" | tee "$VALIDATE_LOG"
run_expecting_failure "$UNCONFIGURED_LOG" \
  dotnet run --project "$CLI_PROJECT" -- pipeline run --template "$TEMPLATE_PATH" --run-id gate-run-unconfigured --format-json
# Test hooks inject extra failures; they must not restore fabricated success
# for the unconfigured default adapter.
ASHLAR_PIPELINE_ENABLE_TEST_HOOKS=1 ASHLAR_PIPELINE_COMPLETION_POLICY=AllowNonCriticalStageFailures \
  run_expecting_failure "$HOOKS_LOG" \
  dotnet run --project "$CLI_PROJECT" -- pipeline run --template "$TEMPLATE_PATH" --run-id gate-run-hooks \
  --input "fail:hybrid:deterministic=true" --format-json
dotnet run --project "$CLI_PROJECT" -- pipeline diagnostics --format-json | tee "$DIAGNOSTICS_LOG"

python3 "$HELPER" fail-closed "$UNCONFIGURED_LOG" unconfigured
python3 "$HELPER" fail-closed "$HOOKS_LOG" test-hooks

echo "== Tier B: cross-process durable resume (LiteDB) =="
RESUME_SOURCE_LOG="$TMP_ROOT/gate-resume-source.log"
RESUME_TARGET_LOG="$TMP_ROOT/gate-resume-target.log"
rm -f "$RESUME_DB"

ASHLAR_PIPELINE_STORE_PROVIDER=LiteDb ASHLAR_PIPELINE_STORE_PATH="$RESUME_DB" ASHLAR_PIPELINE_ENABLE_TEST_HOOKS=1 \
  run_expecting_failure "$RESUME_SOURCE_LOG" \
  dotnet run --project "$CLI_PROJECT" -- pipeline run --template "$TEMPLATE_PATH" \
  --run-id gate-resume-source --input "fail:ingest:deterministic=true" --format-json

ASHLAR_PIPELINE_STORE_PROVIDER=LiteDb ASHLAR_PIPELINE_STORE_PATH="$RESUME_DB" \
  run_expecting_failure "$RESUME_TARGET_LOG" \
  dotnet run --project "$CLI_PROJECT" -- pipeline run --template "$TEMPLATE_PATH" \
  --run-id gate-resume-target --resume-run-id gate-resume-source --resume-failed-stages --format-json

python3 "$HELPER" resume "$RESUME_SOURCE_LOG" "$RESUME_TARGET_LOG"

echo ""
echo "kernel-gate-tier-b: PASS"

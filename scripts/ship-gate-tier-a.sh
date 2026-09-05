#!/usr/bin/env bash
# Ship Tier A: Production Readiness Gate v1 (build + fail-closed CLI + LiteDB resume).
# Mirrors .github/workflows/production-readiness-gate-v1.yml operational steps.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
INFRA_TESTS="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
HELPER="$ROOT/scripts/lib/assert-pipeline-fail-closed.py"

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required for JSON assertions in ship-gate-tier-a" >&2
  exit 1
fi

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

echo "== Ship Tier A: build checks =="
dotnet build src/Ashlar.Core.Application/Ashlar.Core.Application.csproj -f netstandard2.0 -v minimal
dotnet build src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj -v minimal
dotnet build "$CLI" -v minimal

echo "== Ship Tier A: host DI smoke (net8.0, counted) =="
# Two named DI facts. A rename that matches zero still exited 0 before the counted wrapper.
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA_TESTS" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 2 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~HostingE2ESmokeTests.AddAshlar_RegistersObservationPipeline_ByDefault|FullyQualifiedName~PipelineServiceCollectionExtensionsTests.AddAshlar_RegistersPipelineCompositionLayerByDefault" \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none

TMP="$(mktemp -d)"
export TMP
trap 'rm -rf "$TMP"' EXIT
TEMPLATE_PATH="$TMP/pipeline_gate_demo.json"
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

echo "== Ship Tier A: CLI pipeline validate / fail-closed run / diagnostics =="
UNCONFIGURED_LOG="$TMP/gate-run-unconfigured.log"
HOOKS_LOG="$TMP/gate-run-hooks.log"
ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" --no-build -- \
  pipeline validate --template "$TEMPLATE_PATH"
ASHLAR_ALLOW_MOCK=1 \
  run_expecting_failure "$UNCONFIGURED_LOG" \
  dotnet run --project "$CLI" --no-build -- \
  pipeline run --template "$TEMPLATE_PATH" --run-id gate-run-unconfigured --format-json
ASHLAR_ALLOW_MOCK=1 ASHLAR_PIPELINE_ENABLE_TEST_HOOKS=1 ASHLAR_PIPELINE_COMPLETION_POLICY=AllowNonCriticalStageFailures \
  run_expecting_failure "$HOOKS_LOG" \
  dotnet run --project "$CLI" --no-build -- \
  pipeline run --template "$TEMPLATE_PATH" --run-id gate-run-hooks --input "fail:hybrid:deterministic=true" --format-json
ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" --no-build -- pipeline diagnostics --format-json >/dev/null

python3 "$HELPER" fail-closed "$UNCONFIGURED_LOG" unconfigured
python3 "$HELPER" fail-closed "$HOOKS_LOG" test-hooks

echo "== Ship Tier A: LiteDB cross-process resume =="
RESUME_DB="$TMP/ashlar_pipeline_gate_resume.db"
RESUME_SOURCE_LOG="$TMP/gate-resume-source.log"
RESUME_TARGET_LOG="$TMP/gate-resume-target.log"
ASHLAR_ALLOW_MOCK=1 ASHLAR_PIPELINE_STORE_PROVIDER=LiteDb ASHLAR_PIPELINE_STORE_PATH="$RESUME_DB" ASHLAR_PIPELINE_ENABLE_TEST_HOOKS=1 \
  run_expecting_failure "$RESUME_SOURCE_LOG" \
  dotnet run --project "$CLI" --no-build -- \
  pipeline run --template "$TEMPLATE_PATH" --run-id gate-resume-source --input "fail:ingest:deterministic=true" --format-json
ASHLAR_ALLOW_MOCK=1 ASHLAR_PIPELINE_STORE_PROVIDER=LiteDb ASHLAR_PIPELINE_STORE_PATH="$RESUME_DB" \
  run_expecting_failure "$RESUME_TARGET_LOG" \
  dotnet run --project "$CLI" --no-build -- \
  pipeline run --template "$TEMPLATE_PATH" --run-id gate-resume-target --resume-run-id gate-resume-source --resume-failed-stages --format-json

python3 "$HELPER" resume "$RESUME_SOURCE_LOG" "$RESUME_TARGET_LOG"

echo ""
echo "ship-gate-tier-a: PASS"

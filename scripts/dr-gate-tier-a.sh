#!/usr/bin/env bash
# DR Tier A: LiteDB pipeline store backup → wipe → restore → resume (fail-closed).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_PROJECT="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
HELPER="$ROOT/scripts/lib/assert-pipeline-fail-closed.py"
TMP_ROOT="${TMPDIR:-/tmp}/ashlar-dr-gate-$$"
mkdir -p "$TMP_ROOT"
trap 'rm -rf "$TMP_ROOT"' EXIT

DB="$TMP_ROOT/pipeline_dr.db"
BACKUP="$TMP_ROOT/pipeline_dr.backup.db"
TEMPLATE="$TMP_ROOT/pipeline_dr.json"
SOURCE_LOG="$TMP_ROOT/dr-source.log"
RESUME_LOG="$TMP_ROOT/dr-resume.json"

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

cat >"$TEMPLATE" <<'JSON'
{
  "templateId": "dr-gate",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [{ "fromStageId": "ingest", "toStageId": "hybrid" }]
}
JSON

dotnet build "$CLI_PROJECT" -v minimal >/dev/null

echo "== DR Tier A: seed failed run in LiteDB =="
rm -f "$DB" "$BACKUP"
ASHLAR_ALLOW_MOCK=1 ASHLAR_PIPELINE_STORE_PROVIDER=LiteDb ASHLAR_PIPELINE_STORE_PATH="$DB" ASHLAR_PIPELINE_ENABLE_TEST_HOOKS=1 \
  run_expecting_failure "$SOURCE_LOG" \
  dotnet run --project "$CLI_PROJECT" --no-build -- pipeline run --template "$TEMPLATE" \
  --run-id dr-source --input "fail:ingest:deterministic=true" --format-json

if [ ! -f "$DB" ]; then
  echo "DR Tier A: LiteDB file not created" >&2
  exit 1
fi

cp "$DB" "$BACKUP"
rm -f "$DB"

echo "== DR Tier A: restore backup and resume (must stay Failed) =="
cp "$BACKUP" "$DB"
ASHLAR_ALLOW_MOCK=1 ASHLAR_PIPELINE_STORE_PROVIDER=LiteDb ASHLAR_PIPELINE_STORE_PATH="$DB" \
  run_expecting_failure "$RESUME_LOG" \
  dotnet run --project "$CLI_PROJECT" --no-build -- pipeline run --template "$TEMPLATE" \
  --run-id dr-target --resume-run-id dr-source --resume-failed-stages --format-json

python3 "$HELPER" resume "$SOURCE_LOG" "$RESUME_LOG"

REPORT_DIR=".ashlar/dr-gate"
mkdir -p "$REPORT_DIR"
cat >"$REPORT_DIR/pipeline-restore.json" <<EOF
{"ok": true, "store": "LiteDb", "resumeState": "Failed", "durable": true, "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"}
EOF

echo ""
echo "dr-gate-tier-a: PASS"

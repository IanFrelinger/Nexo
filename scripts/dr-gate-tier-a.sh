#!/usr/bin/env bash
# DR Tier A: LiteDB pipeline store backup → wipe → restore → resume.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_PROJECT="application/src/Nexo.CLI/Nexo.CLI.csproj"
TMP_ROOT="${TMPDIR:-/tmp}/nexo-dr-gate-$$"
mkdir -p "$TMP_ROOT"
trap 'rm -rf "$TMP_ROOT"' EXIT

DB="$TMP_ROOT/pipeline_dr.db"
BACKUP="$TMP_ROOT/pipeline_dr.backup.db"
TEMPLATE="$TMP_ROOT/pipeline_dr.json"

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
set +e
NEXO_ALLOW_MOCK=1 NEXO_PIPELINE_STORE_PROVIDER=LiteDb NEXO_PIPELINE_STORE_PATH="$DB" NEXO_PIPELINE_ENABLE_TEST_HOOKS=1 \
  dotnet run --project "$CLI_PROJECT" --no-build -- pipeline run --template "$TEMPLATE" \
  --run-id dr-source --input "fail:ingest:deterministic=true" --format-json >/dev/null
set -e

if [ ! -f "$DB" ]; then
  echo "DR Tier A: LiteDB file not created" >&2
  exit 1
fi

cp "$DB" "$BACKUP"
rm -f "$DB"

echo "== DR Tier A: restore backup and resume =="
cp "$BACKUP" "$DB"
NEXO_ALLOW_MOCK=1 NEXO_PIPELINE_STORE_PROVIDER=LiteDb NEXO_PIPELINE_STORE_PATH="$DB" \
  dotnet run --project "$CLI_PROJECT" --no-build -- pipeline run --template "$TEMPLATE" \
  --run-id dr-target --resume-run-id dr-source --resume-failed-stages --format-json \
  >"$TMP_ROOT/dr-resume.json"

python3 - <<PY
import json
with open("$TMP_ROOT/dr-resume.json", encoding="utf-8") as fh:
    lines = [l.strip() for l in fh if l.strip().startswith("{")]
payload = json.loads(lines[-1])
if not payload.get("ok") or payload.get("data", {}).get("state") != "Completed":
    raise SystemExit(f"resume after restore failed: {payload}")
print("pipeline DR restore+resume: PASS")
PY

REPORT_DIR=".nexo/dr-gate"
mkdir -p "$REPORT_DIR"
cat >"$REPORT_DIR/pipeline-restore.json" <<EOF
{"ok": true, "store": "LiteDb", "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"}
EOF

echo ""
echo "dr-gate-tier-a: PASS"

#!/usr/bin/env bash
# RC Tier D: GitHub Actions RC workflows (verify latest green or dispatch fresh).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

BRANCH="${RC_GATE_GH_BRANCH:-$(git branch --show-current)}"
MAX_AGE_HOURS="${RC_GATE_GH_MAX_AGE_HOURS:-168}"
REPORT_DIR=".nexo/rc-gate"
mkdir -p "$REPORT_DIR"
GH_REPORT="$REPORT_DIR/github-workflows.txt"
: >"$GH_REPORT"

# gh accepts workflow display name or .github/workflows/<file>.yml
WORKFLOWS=(
  "Production Readiness Gate v1"
  "Environment Setup Gate v1"
  "Runtime Release Gate"
  "Installer Bruteforce Gate"
  "Container Image Gate"
  "Onboarding Docs Guard"
)

OPTIONAL_WORKFLOWS=(
  "Runtime Release Promotion"
)

if ! command -v gh >/dev/null 2>&1; then
  echo "== RC Tier D: skipped (gh CLI not installed) =="
  exit 0
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "== RC Tier D: skipped (gh not authenticated) =="
  exit 0
fi

age_ok() {
  local created="$1"
  python3 - <<PY "$created" "$MAX_AGE_HOURS"
import sys
from datetime import datetime, timezone, timedelta
created = sys.argv[1]
max_h = int(sys.argv[2])
if not created:
    sys.exit(1)
ts = datetime.fromisoformat(created.replace("Z", "+00:00"))
ok = datetime.now(timezone.utc) - ts <= timedelta(hours=max_h)
sys.exit(0 if ok else 1)
PY
}

verify_workflow() {
  local wf="$1"
  local required="${2:-1}"
  local json
  json="$(gh run list --workflow "$wf" --branch "$BRANCH" --limit 1 --json databaseId,conclusion,status,createdAt,url 2>/dev/null || true)"
  if [ -z "$json" ] || [ "$json" = "[]" ]; then
    echo "MISSING $wf (no runs on branch $BRANCH)" | tee -a "$GH_REPORT"
    return 1
  fi
  local conclusion status created url
  conclusion="$(echo "$json" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d[0].get('conclusion') or '')")"
  status="$(echo "$json" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d[0].get('status') or '')")"
  created="$(echo "$json" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d[0].get('createdAt') or '')")"
  url="$(echo "$json" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d[0].get('url') or '')")"
  if [ "$status" = "completed" ] && [ "$conclusion" = "success" ] && age_ok "$created"; then
    echo "PASS $wf $url" | tee -a "$GH_REPORT"
    return 0
  fi
  echo "FAIL $wf status=$status conclusion=$conclusion url=$url" | tee -a "$GH_REPORT"
  if [ "$required" = "1" ] && [ "${RC_GATE_TRIGGER_GH:-0}" = "1" ]; then
    echo "  dispatching $wf..." | tee -a "$GH_REPORT"
    gh workflow run "$wf" --ref "$BRANCH" >/dev/null
    local run_id
    run_id="$(gh run list --workflow "$wf" --branch "$BRANCH" --limit 1 --json databaseId -q '.[0].databaseId')"
    gh run watch "$run_id" --exit-status
    echo "PASS $wf (after dispatch)" | tee -a "$GH_REPORT"
    return 0
  fi
  return 1
}

echo "== RC Tier D: GitHub workflow verification (branch=$BRANCH) ==" | tee -a "$GH_REPORT"

fail=0
for wf in "${WORKFLOWS[@]}"; do
  verify_workflow "$wf" 1 || fail=1
done

for wf in "${OPTIONAL_WORKFLOWS[@]}"; do
  verify_workflow "$wf" 0 || echo "NOTE optional workflow not green: $wf (informational)" | tee -a "$GH_REPORT"
done

if [ "${RC_GATE_RUN_PUBLISH:-0}" = "1" ]; then
  verify_workflow "Container Image Publish" 1 || fail=1
else
  echo "SKIP container-image-publish (RC_GATE_RUN_PUBLISH=1 to require)" | tee -a "$GH_REPORT"
fi

if [ "$fail" -ne 0 ]; then
  if [ "${RC_GATE_GH_ADVISORY_ONLY:-0}" = "1" ]; then
    echo "::warning::One or more GitHub workflows not green — see $GH_REPORT"
    echo ""
    echo "rc-gate-tier-d: PASS (advisory)"
    exit 0
  fi
  echo "rc-gate-tier-d: FAIL (see $GH_REPORT)" >&2
  exit 1
fi

echo ""
echo "rc-gate-tier-d: PASS"

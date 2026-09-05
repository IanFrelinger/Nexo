#!/usr/bin/env bash
# Fail closed unless the autonomous release manager verdict is READY for this SHA.
# Used by versioned publish workflows. Never publishes, tags, or mutates remotes.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VERSION="${1:-}"
OUTPUT="${2:-.ashlar/release-manager/current}"

args=(
  --plan ci/autonomous-release-manager.json
  --output "$OUTPUT"
)
if [ -n "$VERSION" ]; then
  args+=(--version "$VERSION")
fi

set +e
python3 scripts/autonomous-release-manager.py "${args[@]}"
rc=$?
set -e

REPORT="$OUTPUT/report.json"
if [ ! -f "$REPORT" ]; then
  echo "Coordinator produced no report; refusing to publish." >&2
  exit 1
fi

verdict="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1])).get("verdict",""))' "$REPORT")"
echo "release-manager verdict=${verdict} exit=${rc}"
if [ "$verdict" != "ready" ] || [ "$rc" -ne 0 ]; then
  echo "Publish is blocked until the autonomous release manager verdict is READY on this SHA (got '${verdict}', exit ${rc})." >&2
  exit 1
fi

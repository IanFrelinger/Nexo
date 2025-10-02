#!/usr/bin/env bash
set -euo pipefail
# Compare two runs' phase outputs using jq-sorted JSON and diff
A="${1:?Run A folder path required}"
B="${2:?Run B folder path required}"
PHASES=(Plan Layout Placement Content Validate)
for p in "${PHASES[@]}"; do
  echo "=== Phase: $p ==="
  if [[ -f "$A/$p/output.json" && -f "$B/$p/output.json" ]]; then
    diff -u <(jq -S . "$A/$p/output.json") <(jq -S . "$B/$p/output.json") || true
  else
    echo "Missing $p output in one of the runs"
  fi
done

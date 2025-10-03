#!/usr/bin/env bash
set -euo pipefail
LG="Artifacts/last_good"
[[ -d "$LG" ]] || { echo "No last_good yet"; exit 1; }
for p in Plan Layout Placement Content Validate; do
  [[ -s "$LG/$p/output.json" ]] || { echo "Missing last_good for $p"; exit 2; }
done
echo "last_good complete"

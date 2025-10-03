#!/usr/bin/env bash
set -euo pipefail

LG="Artifacts/last_good"
if [[ ! -d "$LG" ]]; then
  echo "No last_good directory found."
  exit 1
fi

RC=0
for p in Plan Layout Placement Content Validate; do
  if [[ ! -s "$LG/$p/output.json" ]]; then
    echo "Missing or empty: $LG/$p/output.json"
    RC=2
  else
    echo "OK: $LG/$p/output.json"
  fi
done

exit $RC
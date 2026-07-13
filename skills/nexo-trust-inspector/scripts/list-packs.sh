#!/usr/bin/env bash
set -euo pipefail

ROOT="${1:-}"
if [[ -z "$ROOT" ]]; then
  if [[ -n "${NEXO_TRUST_POLICY_PACKS_PATH:-}" ]]; then
    ROOT="$NEXO_TRUST_POLICY_PACKS_PATH"
  else
    ROOT="config/trust-packs"
  fi
fi

if [[ ! -d "$ROOT" ]]; then
  echo "Trust pack directory not found: $ROOT" >&2
  exit 1
fi

echo "Trust policy packs in $ROOT:"
for file in "$ROOT"/*.json; do
  [[ -f "$file" ]] || continue
  base="$(basename "$file")"
  if [[ "$base" == "active-pack.json" ]]; then
    continue
  fi
  id="$(python3 -c "import json,sys; print(json.load(open(sys.argv[1])).get('id','?'))" "$file")"
  version="$(python3 -c "import json,sys; print(json.load(open(sys.argv[1])).get('version','?'))" "$file")"
  echo "- ${id}@${version} (${base})"
done

if [[ -f "$ROOT/active-pack.json" ]]; then
  echo
  echo "Active pack:"
  cat "$ROOT/active-pack.json"
fi

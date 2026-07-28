#!/usr/bin/env bash
set -euo pipefail
ROOT="${NEXO_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

# An impossible disk threshold must fail closed and still produce diagnostics.
if python3 "$ROOT/scripts/deployment-preflight.py" \
  --workspace "$ROOT" --min-free-gb 999999 --output "$tmp/preflight.json"; then
  echo "expected preflight failure" >&2
  exit 1
fi
python3 - "$tmp/preflight.json" <<'PY'
import json, sys
d=json.load(open(sys.argv[1]))
assert not d["preflight_pass"]
assert d["failures"]
PY

#!/usr/bin/env bash
# Fail if global.json is invalid or no .NET SDK matches the pinned major.minor (release hygiene).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

python3 <<'PY'
import json
from pathlib import Path
p = Path("global.json")
if not p.is_file():
    raise SystemExit("global.json missing")
d = json.loads(p.read_text(encoding="utf-8"))
v = (d.get("sdk") or {}).get("version")
if not v or not isinstance(v, str):
    raise SystemExit("global.json: sdk.version missing")
print(f"global.json sdk.version={v}")
PY

SDK_LINE="$(python3 -c "import json; print(json.load(open('global.json'))['sdk']['version'])")"
MAJ="${SDK_LINE%%.*}"
REST="${SDK_LINE#*.}"
MIN="${REST%%.*}"
PREFIX="${MAJ}.${MIN}."

echo "Expect a listed SDK starting with: ${PREFIX}"
if ! dotnet --list-sdks | grep -q "^${MAJ}\.${MIN}\."; then
  echo "::error::No installed SDK matches global.json major.minor ${MAJ}.${MIN}. Run setup-dotnet with matching SDKs."
  dotnet --list-sdks
  exit 1
fi
echo "verify-release-sdk-pin: OK"

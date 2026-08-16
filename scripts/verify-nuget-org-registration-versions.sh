#!/usr/bin/env bash
# Poll nuget.org registration5-gz-semver2 until each package lists VERSION.
# Env: NEXO_NUGET_VERIFY_VERSION (required), NEXO_NUGET_VERIFY_PACKAGE_IDS (comma; default Bundle/Hosting/Sdk/CLI),
#      NEXO_NUGET_VERIFY_ATTEMPTS, NEXO_NUGET_VERIFY_SLEEP_SEC
set -euo pipefail
VER="${NEXO_NUGET_VERIFY_VERSION:?set NEXO_NUGET_VERIFY_VERSION}"
VER="${VER#v}"
ATTEMPTS="${NEXO_NUGET_VERIFY_ATTEMPTS:-12}"
SLEEP_SEC="${NEXO_NUGET_VERIFY_SLEEP_SEC:-15}"
[[ -z "${ATTEMPTS}" ]] && ATTEMPTS=12
[[ -z "${SLEEP_SEC}" ]] && SLEEP_SEC=15

if [[ -n "${NEXO_NUGET_VERIFY_PACKAGE_IDS:-}" && "${NEXO_NUGET_VERIFY_PACKAGE_IDS}" != "" ]]; then
  mapfile -t IDS < <(echo "${NEXO_NUGET_VERIFY_PACKAGE_IDS}" | tr ',' '\n' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//' | grep -v '^$' || true)
else
  IDS=(Nexo.Hosting.Bundle Nexo.Hosting Nexo.Sdk Nexo.CLI)
fi

reg_has_version() {
  python3 -c "
import json, sys, urllib.request

pid, want = sys.argv[1], sys.argv[2]
url = f'https://api.nuget.org/v3/registration5-gz-semver2/{pid.lower()}/index.json'
try:
    raw = urllib.request.urlopen(url, timeout=60).read()
except Exception:
    sys.exit(1)
data = json.loads(raw.decode('utf-8'))
versions = set()

def walk(o):
    if isinstance(o, dict):
        ce = o.get('catalogEntry')
        if isinstance(ce, dict) and 'version' in ce:
            versions.add(ce['version'])
        for v in o.values():
            walk(v)
    elif isinstance(o, list):
        for v in o:
            walk(v)

walk(data)
sys.exit(0 if want in versions else 1)
" "$1" "$2"
}

echo "Registration API: waiting for ${VER} on: ${IDS[*]}"

for i in $(seq 1 "${ATTEMPTS}"); do
  ok=1
  for ID in "${IDS[@]}"; do
    [[ -z "$ID" ]] && continue
    if ! reg_has_version "$ID" "$VER"; then
      ok=0
      echo "  attempt ${i}/${ATTEMPTS}: ${ID} — version ${VER} not in registration yet"
    fi
  done
  if [[ "$ok" -eq 1 ]]; then
    echo "verify-nuget-org-registration-versions: OK (attempt ${i}/${ATTEMPTS})"
    exit 0
  fi
  [[ "$i" -lt "${ATTEMPTS}" ]] && sleep "${SLEEP_SEC}"
done

echo "::error::Registration API did not list ${VER} for all packages after ${ATTEMPTS} attempts."
exit 1

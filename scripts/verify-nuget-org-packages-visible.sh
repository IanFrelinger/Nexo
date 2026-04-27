#!/usr/bin/env bash
# Poll nuget.org flat-container until all listed package versions return HTTP 200.
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
  IDS=(Nexo.Hosting.Bundle Nexo.Hosting Nexo.Sdk)
fi

http_head() {
  curl -sS -o /dev/null -w "%{http_code}" -I --max-time 30 "$1" || echo "000"
}

echo "Flat-container: ${VER} — ${IDS[*]}"

for i in $(seq 1 "${ATTEMPTS}"); do
  ok=1
  for ID in "${IDS[@]}"; do
    [[ -z "$ID" ]] && continue
    id_lc="$(echo "$ID" | tr '[:upper:]' '[:lower:]')"
    ver_lc="$(echo "$VER" | tr '[:upper:]' '[:lower:]')"
    url="https://api.nuget.org/v3-flatcontainer/${id_lc}/${ver_lc}/${id_lc}.${ver_lc}.nupkg"
    code="$(http_head "$url")"
    if [[ "$code" != "200" ]]; then
      ok=0
      echo "  attempt ${i}/${ATTEMPTS}: ${ID} HTTP ${code}"
    fi
  done
  if [[ "$ok" -eq 1 ]]; then
    echo "verify-nuget-org-packages-visible: OK"
    exit 0
  fi
  [[ "$i" -lt "${ATTEMPTS}" ]] && sleep "${SLEEP_SEC}"
done
echo "::error::flat-container not ready"
exit 1

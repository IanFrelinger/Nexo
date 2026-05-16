#!/usr/bin/env bash
# Poll nuget.org flat container until all listed package versions return HTTP 200 (index lag after push).
#
# NEXO_NUGET_VERIFY_VERSION — required semver (no v prefix)
# NEXO_NUGET_VERIFY_PACKAGE_IDS — comma-separated ids (default: Nexo.Hosting.Bundle,Nexo.Hosting,Nexo.Sdk)
# NEXO_NUGET_VERIFY_PACKAGE_ID — if set and NEXO_NUGET_VERIFY_PACKAGE_IDS unset, a single id (backward compat)
# NEXO_NUGET_VERIFY_ATTEMPTS / NEXO_NUGET_VERIFY_SLEEP_SEC — optional (defaults 12 / 15)
set -euo pipefail
VER="${NEXO_NUGET_VERIFY_VERSION:?set NEXO_NUGET_VERIFY_VERSION (semver, no v prefix)}"
VER="${VER#v}"
ATTEMPTS="${NEXO_NUGET_VERIFY_ATTEMPTS:-12}"
SLEEP_SEC="${NEXO_NUGET_VERIFY_SLEEP_SEC:-15}"
# GitHub Actions may pass empty strings for unset repo variables; treat as defaults.
[[ -z "${ATTEMPTS}" ]] && ATTEMPTS=12
[[ -z "${SLEEP_SEC}" ]] && SLEEP_SEC=15

# GitHub Actions may set NEXO_NUGET_VERIFY_PACKAGE_IDS to empty when the repo var is unset; treat as default.
if [[ -n "${NEXO_NUGET_VERIFY_PACKAGE_IDS:-}" && "${NEXO_NUGET_VERIFY_PACKAGE_IDS}" != "" ]]; then
  mapfile -t IDS < <(echo "${NEXO_NUGET_VERIFY_PACKAGE_IDS}" | tr ',' '\n' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//' | grep -v '^$' || true)
elif [[ -n "${NEXO_NUGET_VERIFY_PACKAGE_ID:-}" ]]; then
  IDS=("${NEXO_NUGET_VERIFY_PACKAGE_ID}")
else
  IDS=(Nexo.Hosting.Bundle Nexo.Hosting Nexo.Sdk)
fi

if [[ "${#IDS[@]}" -eq 0 ]]; then
  echo "::error::No package ids to verify (set NEXO_NUGET_VERIFY_PACKAGE_IDS or NEXO_NUGET_VERIFY_PACKAGE_ID)."
  exit 1
fi

echo "Checking nuget.org visibility for version ${VER}: ${IDS[*]}"
echo "Attempts=${ATTEMPTS} sleep=${SLEEP_SEC}s"

http_head() {
  local url="$1"
  curl -sS -o /dev/null -w "%{http_code}" -I --max-time 30 "$url" || echo "000"
}

for i in $(seq 1 "${ATTEMPTS}"); do
  all_ok=1
  for ID in "${IDS[@]}"; do
    [[ -z "$ID" ]] && continue
    id_lc="$(echo "$ID" | tr '[:upper:]' '[:lower:]')"
    ver_lc="$(echo "$VER" | tr '[:upper:]' '[:lower:]')"
    url="https://api.nuget.org/v3-flatcontainer/${id_lc}/${ver_lc}/${id_lc}.${ver_lc}.nupkg"
    code="$(http_head "$url")"
    if [[ "$code" != "200" ]]; then
      all_ok=0
      echo "  attempt ${i}/${ATTEMPTS}: ${ID} HTTP ${code} (${url})"
    fi
  done
  if [[ "$all_ok" -eq 1 ]]; then
    echo "verify-nuget-org-packages-visible: OK (attempt ${i}/${ATTEMPTS})"
    exit 0
  fi
  if [[ "$i" -lt "${ATTEMPTS}" ]]; then
    sleep "${SLEEP_SEC}"
  fi
done

echo "::error::One or more packages not visible on nuget.org for version ${VER} after ${ATTEMPTS} attempts."
exit 1

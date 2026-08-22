#!/usr/bin/env bash
# Run verify-nuget-org-restore-published-version.sh with empty NUGET_PACKAGES + DOTNET_CLI_HOME (no stale global cache).
# Same env: ASHLAR_NUGET_RESTORE_VERIFY_VERSION (required). Optional ASHLAR_NUGET_VERIFY_NO_ISOLATED_CACHE=1 to use caller env.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ -n "${ASHLAR_NUGET_VERIFY_NO_ISOLATED_CACHE:-}" ]]; then
  exec bash "${ROOT}/scripts/verify-nuget-org-restore-published-version.sh"
fi
BASE="${ASHLAR_NUGET_VERIFY_ISOLATED_ROOT:-}"
if [[ -z "${BASE}" ]]; then
  BASE="$(mktemp -d "${TMPDIR:-/tmp}/ashlar-nuget-org-restore-isol-XXXXXX")"
  trap 'rm -rf "${BASE}"' EXIT
else
  mkdir -p "${BASE}"
fi
mkdir -p "${BASE}/packages" "${BASE}/cli-home"
export NUGET_PACKAGES="${BASE}/packages"
export DOTNET_CLI_HOME="${BASE}/cli-home"
echo "Isolated nuget.org restore: NUGET_PACKAGES=${NUGET_PACKAGES} DOTNET_CLI_HOME=${DOTNET_CLI_HOME}"
bash "${ROOT}/scripts/verify-nuget-org-restore-published-version.sh"

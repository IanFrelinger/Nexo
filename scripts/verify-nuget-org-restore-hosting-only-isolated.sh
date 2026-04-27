#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ -n "${NEXO_NUGET_VERIFY_NO_ISOLATED_CACHE:-}" ]]; then
  exec bash "${ROOT}/scripts/verify-nuget-org-restore-hosting-only.sh"
fi
BASE="$(mktemp -d "${TMPDIR:-/tmp}/nexo-nuget-hosting-isol-XXXXXX")"
trap 'rm -rf "${BASE}"' EXIT
mkdir -p "${BASE}/packages" "${BASE}/cli-home"
export NUGET_PACKAGES="${BASE}/packages"
export DOTNET_CLI_HOME="${BASE}/cli-home"
bash "${ROOT}/scripts/verify-nuget-org-restore-hosting-only.sh"

#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

echo "============================================="
echo " Nexo Linux Zero-to-Hero Installer"
echo "============================================="
echo ""
echo "This runs one-click setup, verifies CLI, and runs"
echo "a first pipeline validate/run flow."
echo ""

if [[ ! -f "${REPO_ROOT}/scripts/install/install.sh" ]]; then
  echo "Error: expected installer at scripts/install/install.sh"
  echo "Please run this file from a full Nexo repository checkout."
  exit 1
fi

bash "${REPO_ROOT}/scripts/install/install.sh" --yes --hero "$@"
echo ""
echo "Done. Next commands:"
echo "  cd \"${HOME}/Nexo\""
echo "  dotnet run --project src/Nexo.CLI -- --help"

#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OS="$(uname -s)"

case "${OS}" in
  Linux)
    exec env NEXO_INSTALL_PLATFORM=linux bash "${SCRIPT_DIR}/install_common.sh" "$@"
    ;;
  Darwin)
    exec env NEXO_INSTALL_PLATFORM=darwin bash "${SCRIPT_DIR}/install_common.sh" "$@"
    ;;
  *)
    echo "Unsupported OS for install.sh: ${OS}" >&2
    echo "Use scripts/install/install.ps1 on Windows." >&2
    exit 1
    ;;
esac

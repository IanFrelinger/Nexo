#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OS="$(uname -s)"

case "${OS}" in
  Linux)
    exec "${SCRIPT_DIR}/setup-linux.sh" "$@"
    ;;
  Darwin)
    exec "${SCRIPT_DIR}/setup-macos.sh" "$@"
    ;;
  *)
    echo "Unsupported OS for setup.sh: ${OS}" >&2
    echo "Use scripts/setup/setup-windows.ps1 on Windows." >&2
    exit 1
    ;;
esac

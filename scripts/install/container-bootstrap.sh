#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OS="$(uname -s)"

case "${OS}" in
  Linux)
    exec "${SCRIPT_DIR}/container-bootstrap-linux.sh" "$@"
    ;;
  Darwin)
    exec "${SCRIPT_DIR}/container-bootstrap-macos.sh" "$@"
    ;;
  *)
    echo "Unsupported OS for container-bootstrap.sh: ${OS}" >&2
    echo "Use scripts/install/container-bootstrap.ps1 on Windows." >&2
    exit 1
    ;;
esac

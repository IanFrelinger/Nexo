#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OS="$(uname -s)"

show_intro() {
  echo "============================================="
  echo " Nexo One-Click Setup"
  echo "============================================="
  echo ""
  echo "This installer will:"
  echo "  1) install missing prerequisites (git, curl, .NET SDK 9+)"
  echo "  2) clone/update Nexo"
  echo "  3) restore + build the CLI"
  echo "  4) optionally run first-use verification (--hero)"
  echo ""
  echo "You do NOT need to know package managers."
  echo ""
}

main() {
  show_intro
  case "${OS}" in
    Linux)
      exec "${SCRIPT_DIR}/install-linux.sh" --yes --hero "$@"
      ;;
    Darwin)
      exec "${SCRIPT_DIR}/install-macos.sh" --yes --hero "$@"
      ;;
    *)
      echo "Unsupported OS for one-click.sh: ${OS}" >&2
      echo "On Windows run:" >&2
      echo "  powershell -NoProfile -ExecutionPolicy Bypass -File .\\scripts\\install\\one-click.ps1" >&2
      exit 1
      ;;
  esac
}

main "$@"

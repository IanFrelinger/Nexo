#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "============================================="
echo " Nexo Native Installer (macOS)"
echo "============================================="
echo ""

if ! bash "${SCRIPT_DIR}/install.sh"; then
  echo ""
  echo "Install failed."
  read -r -p "Press Enter to close this window..."
  exit 1
fi

echo ""
echo "Install complete."
read -r -p "Press Enter to close this window..."

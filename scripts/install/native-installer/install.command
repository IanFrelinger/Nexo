#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
AUTO_CLOSE=false

for arg in "$@"; do
  case "${arg}" in
    --no-pause)
      AUTO_CLOSE=true
      ;;
  esac
done

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
if [[ "${AUTO_CLOSE}" != "true" ]]; then
  read -r -p "Press Enter to close this window..."
fi

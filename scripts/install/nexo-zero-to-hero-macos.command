#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
AUTO_CLOSE=false
FORWARD_ARGS=()

for arg in "$@"; do
  case "${arg}" in
    --no-pause)
      AUTO_CLOSE=true
      ;;
    *)
      FORWARD_ARGS+=("${arg}")
      ;;
  esac
done

echo "============================================="
echo " Nexo macOS Zero-to-Hero Installer"
echo "============================================="
echo ""
echo "This will run one-click setup, verify CLI, and run"
echo "a first pipeline validate command."
echo ""

if [[ ! -f "${REPO_ROOT}/scripts/install/install.sh" ]]; then
  echo "Error: expected installer at scripts/install/install.sh"
  echo "Please run this file from a full Nexo repository checkout."
  exit 1
fi

EXIT_CODE=0
if ! bash "${REPO_ROOT}/scripts/install/install.sh" --yes --hero "${FORWARD_ARGS[@]}"; then
  EXIT_CODE=$?
  echo ""
  echo "Setup finished with errors. Scroll up in this window for details."
else
  echo ""
  echo "Setup completed successfully."
fi

echo ""
echo "If this Terminal window closes, open it again and run:"
echo "  cd \"${HOME}/Nexo\""
echo "  dotnet run --project src/Nexo.CLI -- --help"

if [[ "${AUTO_CLOSE}" != "true" ]]; then
  echo ""
  read -r -p "Press Enter to close this window..."
fi

exit "${EXIT_CODE}"

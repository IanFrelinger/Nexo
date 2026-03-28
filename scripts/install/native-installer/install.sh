#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_BASE="${NEXO_INSTALL_BASE:-${HOME}/.local/bin}"
TARGET_PATH="${INSTALL_BASE}/nexo"
SOURCE_PATH="${SCRIPT_DIR}/bin/nexo"

if [[ ! -f "${SOURCE_PATH}" ]]; then
  echo "Missing bundled binary at ${SOURCE_PATH}" >&2
  exit 1
fi

mkdir -p "${INSTALL_BASE}"
cp "${SOURCE_PATH}" "${TARGET_PATH}"
chmod +x "${TARGET_PATH}"

echo "Installed nexo to ${TARGET_PATH}"

case ":${PATH}:" in
  *":${INSTALL_BASE}:"*)
    ;;
  *)
    echo ""
    echo "PATH update recommended: add ${INSTALL_BASE}"
    echo "Example:"
    echo "  echo 'export PATH=\"${INSTALL_BASE}:\$PATH\"' >> \"${HOME}/.bashrc\""
    ;;
esac

echo ""
echo "Verify with:"
echo "  ${TARGET_PATH} --help"

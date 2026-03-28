#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_BASE="${NEXO_INSTALL_BASE:-${HOME}/.local/share/nexo}"
BIN_DIR="${INSTALL_BASE}/bin"
TARGET_PATH="${BIN_DIR}/nexo"
SOURCE_BIN_DIR="${SCRIPT_DIR}/bin"
SOURCE_PATH="${SOURCE_BIN_DIR}/nexo"

if [[ ! -d "${SOURCE_BIN_DIR}" ]]; then
  echo "Missing bundled app directory at ${SOURCE_BIN_DIR}" >&2
  exit 1
fi

mkdir -p "${BIN_DIR}"
cp -R "${SOURCE_BIN_DIR}/." "${BIN_DIR}/"
chmod +x "${TARGET_PATH}"

echo "Installed nexo to ${BIN_DIR}"

case ":${PATH}:" in
  *":${BIN_DIR}:"*)
    ;;
  *)
    echo ""
    echo "PATH update recommended: add ${BIN_DIR}"
    echo "Example:"
    echo "  echo 'export PATH=\"${BIN_DIR}:\$PATH\"' >> \"${HOME}/.bashrc\""
    ;;
esac

echo ""
echo "Verify with:"
echo "  ${TARGET_PATH} --help"

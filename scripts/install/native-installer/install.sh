#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_BIN_DIR="${NEXO_INSTALL_BIN:-${HOME}/.local/bin}"
INSTALL_APP_DIR="${NEXO_INSTALL_APP:-${HOME}/.local/lib/nexo}"
TARGET_BIN_PATH="${INSTALL_BIN_DIR}/nexo"
SOURCE_APP_DIR="${SCRIPT_DIR}/app"
TARGET_APP_BINARY="${INSTALL_APP_DIR}/Nexo.CLI"

if [[ ! -d "${SOURCE_APP_DIR}" ]]; then
  echo "Missing bundled app directory at ${SOURCE_APP_DIR}" >&2
  exit 1
fi

mkdir -p "$(dirname "${INSTALL_APP_DIR}")"
rm -rf "${INSTALL_APP_DIR}"
cp -R "${SOURCE_APP_DIR}" "${INSTALL_APP_DIR}"

mkdir -p "${INSTALL_BIN_DIR}"
cat > "${TARGET_BIN_PATH}" <<EOF
#!/usr/bin/env bash
set -euo pipefail
exec "${TARGET_APP_BINARY}" "\$@"
EOF
chmod +x "${TARGET_BIN_PATH}"

echo "Installed nexo launcher to ${TARGET_BIN_PATH}"
echo "Installed app to ${INSTALL_APP_DIR}"

case ":${PATH}:" in
  *":${INSTALL_BIN_DIR}:"*)
    ;;
  *)
    echo ""
    echo "PATH update recommended: add ${INSTALL_BIN_DIR}"
    echo "Example:"
    echo "  echo 'export PATH=\"${INSTALL_BIN_DIR}:\$PATH\"' >> \"${HOME}/.bashrc\""
    ;;
esac

echo ""
echo "Verify with:"
echo "  ${TARGET_BIN_PATH} --help"

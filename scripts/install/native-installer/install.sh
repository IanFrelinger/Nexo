#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_BIN_DIR="${NEXO_INSTALL_BIN:-${HOME}/.local/bin}"
INSTALL_APP_DIR="${NEXO_INSTALL_APP:-${HOME}/.local/lib/nexo}"
TARGET_BIN_PATH="${INSTALL_BIN_DIR}/nexo"
SOURCE_APP_DIR="${SCRIPT_DIR}/app"
TARGET_APP_BINARY="${INSTALL_APP_DIR}/Nexo.CLI"
PATH_EXPORT_LINE="export PATH=\"${INSTALL_BIN_DIR}:\$PATH\""
INSTALL_MARKER="# Added by Nexo native installer"
PATH_LINE_ADDED=false

if [[ ! -d "${SOURCE_APP_DIR}" ]]; then
  echo "Missing bundled app directory at ${SOURCE_APP_DIR}" >&2
  exit 1
fi

append_path_if_needed() {
  local profile_path="$1"
  local marker_present=false
  local export_present=false

  if [[ -f "${profile_path}" ]]; then
    while IFS= read -r line; do
      [[ "${line}" == "${INSTALL_MARKER}" ]] && marker_present=true
      [[ "${line}" == "${PATH_EXPORT_LINE}" ]] && export_present=true
    done < "${profile_path}"
  fi

  if [[ "${export_present}" == "true" ]]; then
    PATH_LINE_ADDED=false
    return
  fi

  {
    echo ""
    if [[ "${marker_present}" != "true" ]]; then
      echo "${INSTALL_MARKER}"
    fi
    echo "${PATH_EXPORT_LINE}"
  } >> "${profile_path}"
  PATH_LINE_ADDED=true
}

mkdir -p "$(dirname "${INSTALL_APP_DIR}")"
tmp_install_dir="${INSTALL_APP_DIR}.tmp.$$"
rm -rf "${tmp_install_dir}"
cp -R "${SOURCE_APP_DIR}" "${tmp_install_dir}"
chmod +x "${tmp_install_dir}/Nexo.CLI"
rm -rf "${INSTALL_APP_DIR}"
mv "${tmp_install_dir}" "${INSTALL_APP_DIR}"

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
    target_profile=""
    for candidate in "${HOME}/.bashrc" "${HOME}/.zshrc" "${HOME}/.profile"; do
      if [[ -f "${candidate}" ]]; then
        target_profile="${candidate}"
        break
      fi
    done
    if [[ -z "${target_profile}" ]]; then
      target_profile="${HOME}/.profile"
    fi
    append_path_if_needed "${target_profile}"
    echo ""
    if [[ "${PATH_LINE_ADDED}" == "true" ]]; then
      echo "Added ${INSTALL_BIN_DIR} to PATH in ${target_profile}"
      echo "Restart your terminal (or run: source \"${target_profile}\") to use 'nexo' directly."
    else
      echo "PATH already configured for ${INSTALL_BIN_DIR} in ${target_profile}"
    fi
    ;;
esac

if ! "${TARGET_BIN_PATH}" --version >/dev/null 2>&1; then
  echo "Install verification failed: unable to run ${TARGET_BIN_PATH} --version" >&2
  exit 1
fi

echo ""
echo "Verification passed:"
echo "  ${TARGET_BIN_PATH} --version"

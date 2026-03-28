#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
TEMPLATE_DIR="${SCRIPT_DIR}/native-installer"

RID=""
PLATFORM=""
VERSION="dev"
OUTPUT_DIR=""

usage() {
  echo "Usage: scripts/install/package-native-bundle.sh --rid <rid> --platform <linux|macos> --output-dir <dir> [--version <version>]"
}

require_value() {
  local flag="$1"
  local value="${2:-}"
  if [[ -z "${value}" || "${value}" == --* ]]; then
    echo "Missing value for ${flag}" >&2
    exit 1
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --rid)
      require_value "$1" "${2:-}"
      RID="$2"
      shift
      ;;
    --platform)
      require_value "$1" "${2:-}"
      PLATFORM="$2"
      shift
      ;;
    --output-dir)
      require_value "$1" "${2:-}"
      OUTPUT_DIR="$2"
      shift
      ;;
    --version)
      require_value "$1" "${2:-}"
      VERSION="$2"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
  shift
done

if [[ -z "${RID}" || -z "${PLATFORM}" || -z "${OUTPUT_DIR}" ]]; then
  usage
  exit 1
fi

if [[ "${PLATFORM}" != "linux" && "${PLATFORM}" != "macos" ]]; then
  echo "Unsupported platform: ${PLATFORM}" >&2
  exit 1
fi

if [[ ! -d "${TEMPLATE_DIR}" ]]; then
  echo "Missing template directory: ${TEMPLATE_DIR}" >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is required to package installer bundles." >&2
  exit 1
fi

mkdir -p "${OUTPUT_DIR}"

publish_dir="$(mktemp -d)"
work_dir="$(mktemp -d)"
cleanup() {
  rm -rf "${publish_dir}" "${work_dir}"
}
trap cleanup EXIT

echo "Publishing self-contained CLI for ${RID}..."
dotnet publish "${REPO_ROOT}/src/Nexo.CLI/Nexo.CLI.csproj" \
  -c Release \
  -r "${RID}" \
  --self-contained true \
  -p:IncludeTestProjectReferences=false \
  -o "${publish_dir}"

source_binary="${publish_dir}/Nexo.CLI"
if [[ ! -f "${source_binary}" ]]; then
  echo "Expected publish output not found: ${source_binary}" >&2
  exit 1
fi

bundle_name="nexo-native-installer-${PLATFORM}-${RID}"
bundle_dir="${work_dir}/${bundle_name}"
mkdir -p "${bundle_dir}"

cp -R "${publish_dir}" "${bundle_dir}/app"
chmod +x "${bundle_dir}/app/Nexo.CLI"

cp "${TEMPLATE_DIR}/install.sh" "${bundle_dir}/install.sh"
chmod +x "${bundle_dir}/install.sh"

if [[ "${PLATFORM}" == "macos" ]]; then
  cp "${TEMPLATE_DIR}/install.command" "${bundle_dir}/install.command"
  chmod +x "${bundle_dir}/install.command"
fi

cat > "${bundle_dir}/README.md" <<EOF
# Nexo Native Installer (${PLATFORM} / ${RID})

Version: ${VERSION}

This bundle contains a self-contained \`nexo\` CLI app directory and an install wrapper.

## Install

- Linux/macOS terminal:

\`\`\`bash
./install.sh
\`\`\`

- macOS Finder (double-click):

\`\`\`bash
./install.command
\`\`\`

## What install does

1. Copies \`bin/nexo\` to \`\$HOME/.local/bin/nexo\`.
2. Copies runtime dependencies to \`\$HOME/.local/lib/nexo\`.
3. Marks \`nexo\` executable.
4. Prints PATH guidance if \`\$HOME/.local/bin\` is not currently on PATH.

## Verify

\`\`\`bash
nexo --help
\`\`\`
EOF

artifact_name="${bundle_name}-${VERSION}.tar.gz"
artifact_path="${OUTPUT_DIR}/${artifact_name}"

tar -C "${work_dir}" -czf "${artifact_path}" "${bundle_name}"
echo "Created: ${artifact_path}"

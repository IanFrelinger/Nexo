#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OS="$(uname -s)"

YES=false
DRY_RUN=false
WORKSPACE_DIR="${PWD}"
WITH_SDK=true
IMAGE=""
SDK_IMAGE=""
DAEMON_DURATION=""

usage() {
  echo "Usage: scripts/install/container-one-click.sh [options]"
  echo ""
  echo "Options:"
  echo "  --workspace <path>        Workspace mount path (default: current directory)"
  echo "  --image <ref>             Override CLI container image"
  echo "  --sdk-image <ref>         Override SDK container image"
  echo "  --skip-sdk                Skip SDK image smoke checks"
  echo "  --start-daemon <duration> Start a bounded background-agent daemon smoke run"
  echo "  --yes                     Auto-confirm prompts"
  echo "  --dry-run                 Print actions without executing"
  echo "  -h, --help                Show help"
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
    --workspace)
      require_value "$1" "${2:-}"
      WORKSPACE_DIR="$2"
      shift
      ;;
    --image)
      require_value "$1" "${2:-}"
      IMAGE="$2"
      shift
      ;;
    --sdk-image)
      require_value "$1" "${2:-}"
      SDK_IMAGE="$2"
      shift
      ;;
    --skip-sdk)
      WITH_SDK=false
      ;;
    --start-daemon)
      require_value "$1" "${2:-}"
      DAEMON_DURATION="$2"
      shift
      ;;
    --yes)
      YES=true
      ;;
    --dry-run)
      DRY_RUN=true
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

echo "============================================="
echo " Nexo One-Click Container Setup"
echo "============================================="
echo ""
echo "This setup will:"
echo "  1) ensure Docker is installed and running"
echo "  2) pull the Nexo runtime container"
if [[ "${WITH_SDK}" == "true" ]]; then
  echo "  3) pull/test the SDK container"
fi
echo "  4) run container smoke checks against your workspace"
echo ""
echo "You do NOT need to know Docker commands."
echo ""

if [[ -n "${DAEMON_DURATION}" ]]; then
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] docker run --rm -v \"${WORKSPACE_DIR}:/work\" -w /work \"${IMAGE:-ghcr.io/ianfrelinger/nexo-cli:latest}\" background-agent daemon --duration \"${DAEMON_DURATION}\""
  else
    daemon_image="${IMAGE:-ghcr.io/ianfrelinger/nexo-cli:latest}"
    echo "Running container background-agent daemon smoke test (${DAEMON_DURATION})..."
    docker run --rm -v "${WORKSPACE_DIR}:/work" -w /work "${daemon_image}" background-agent daemon --duration "${DAEMON_DURATION}"
  fi
  echo ""
fi

bootstrap_args=()
if [[ "${YES}" == "true" ]]; then
  bootstrap_args+=(--yes)
fi
if [[ "${DRY_RUN}" == "true" ]]; then
  bootstrap_args+=(--dry-run)
fi
if [[ -n "${WORKSPACE_DIR}" ]]; then
  bootstrap_args+=(--workspace "${WORKSPACE_DIR}")
fi
if [[ -n "${IMAGE}" ]]; then
  bootstrap_args+=(--image "${IMAGE}")
fi
if [[ "${WITH_SDK}" == "true" && -n "${SDK_IMAGE}" ]]; then
  bootstrap_args+=(--sdk-image "${SDK_IMAGE}")
fi

case "${OS}" in
  Linux)
    exec bash "${SCRIPT_DIR}/container-bootstrap-linux.sh" "${bootstrap_args[@]}"
    ;;
  Darwin)
    exec bash "${SCRIPT_DIR}/container-bootstrap-macos.sh" "${bootstrap_args[@]}"
    ;;
  *)
    echo "Unsupported OS for container-one-click.sh: ${OS}" >&2
    echo "Use scripts/install/container-one-click.ps1 on Windows." >&2
    exit 1
    ;;
esac

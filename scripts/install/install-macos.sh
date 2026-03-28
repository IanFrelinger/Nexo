#!/usr/bin/env bash
set -euo pipefail

DEFAULT_REPO_URL="https://github.com/IanFrelinger/Nexo.git"
DEFAULT_INSTALL_DIR="${HOME}/Nexo"

REPO_URL="${DEFAULT_REPO_URL}"
INSTALL_DIR="${DEFAULT_INSTALL_DIR}"
BRANCH=""
INCLUDE_OPTIONAL=false
YES=false
SKIP_BUILD=false
RUN_CONTAINER_SMOKE=false
DRY_RUN=false

usage() {
  echo "Usage: scripts/install/install-macos.sh [options]"
  echo ""
  echo "Options:"
  echo "  --repo-url <url>          Git repository URL (default: ${DEFAULT_REPO_URL})"
  echo "  --install-dir <path>      Installation directory (default: ${DEFAULT_INSTALL_DIR})"
  echo "  --branch <name>           Optional git branch/tag to checkout"
  echo "  --include-optional        Include optional dependencies in setup apply"
  echo "  --yes                     Auto-confirm dependency installation prompts"
  echo "  --skip-build              Skip CLI build smoke step"
  echo "  --run-container-smoke     Run container smoke after native setup (requires Docker)"
  echo "  --dry-run                 Print actions without executing"
  echo "  -h, --help                Show help"
}

die() {
  echo "$1" >&2
  exit 1
}

require_value() {
  local flag="$1"
  local value="${2:-}"
  if [[ -z "${value}" || "${value}" == --* ]]; then
    die "Missing value for ${flag}"
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-url)
      require_value "$1" "${2:-}"
      REPO_URL="$2"
      shift
      ;;
    --install-dir)
      require_value "$1" "${2:-}"
      INSTALL_DIR="$2"
      shift
      ;;
    --branch)
      require_value "$1" "${2:-}"
      BRANCH="$2"
      shift
      ;;
    --include-optional)
      INCLUDE_OPTIONAL=true
      ;;
    --yes)
      YES=true
      ;;
    --skip-build)
      SKIP_BUILD=true
      ;;
    --run-container-smoke)
      RUN_CONTAINER_SMOKE=true
      ;;
    --dry-run)
      DRY_RUN=true
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      die "Unknown argument: $1"
      ;;
  esac
  shift
done

require_macos() {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    die "This installer only supports macOS hosts."
  fi
}

expand_install_dir() {
  if [[ "${INSTALL_DIR}" == "~" ]]; then
    INSTALL_DIR="${HOME}"
  elif [[ "${INSTALL_DIR}" == ~/* ]]; then
    INSTALL_DIR="${HOME}/${INSTALL_DIR#~/}"
  fi
}

run_cmd() {
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] $*"
    return 0
  fi
  "$@"
}

run_in_repo() {
  local target_dir="$1"
  shift
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] (cd \"${target_dir}\" && $*)"
    return 0
  fi
  (
    cd "${target_dir}"
    "$@"
  )
}

sync_repo() {
  local target_dir="$1"
  if [[ -d "${target_dir}/.git" ]]; then
    run_cmd git -C "${target_dir}" fetch --all --tags
    if [[ -n "${BRANCH}" ]]; then
      run_cmd git -C "${target_dir}" checkout "${BRANCH}"
      run_cmd git -C "${target_dir}" pull --ff-only origin "${BRANCH}"
    else
      run_cmd git -C "${target_dir}" pull --ff-only
    fi
    return
  fi

  run_cmd mkdir -p "$(dirname "${target_dir}")"
  if [[ -n "${BRANCH}" ]]; then
    run_cmd git clone --branch "${BRANCH}" --single-branch "${REPO_URL}" "${target_dir}"
  else
    run_cmd git clone "${REPO_URL}" "${target_dir}"
  fi
}

run_setup() {
  local target_dir="$1"
  local setup_args=(scripts/setup/setup.sh apply)
  if [[ "${INCLUDE_OPTIONAL}" == "true" ]]; then
    setup_args+=(--include-optional)
  fi
  if [[ "${YES}" == "true" ]]; then
    setup_args+=(--yes)
  fi

  run_in_repo "${target_dir}" bash "${setup_args[@]}"
  run_in_repo "${target_dir}" bash scripts/setup/setup.sh restore
}

run_build() {
  local target_dir="$1"
  if [[ "${SKIP_BUILD}" == "true" ]]; then
    return
  fi
  run_in_repo "${target_dir}" dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
}

run_container_smoke() {
  local target_dir="$1"
  if [[ "${RUN_CONTAINER_SMOKE}" != "true" ]]; then
    return
  fi

  local image="ghcr.io/ianfrelinger/nexo-cli:latest"
  run_in_repo "${target_dir}" docker pull "${image}"
  run_in_repo "${target_dir}" docker run --rm "${image}" --help
  run_in_repo "${target_dir}" docker run --rm -v "${target_dir}:/work" -w /work "${image}" --help
}

print_next_steps() {
  local target_dir="$1"
  echo ""
  echo "Install complete."
  echo "Repo: ${target_dir}"
  echo ""
  echo "Next commands:"
  echo "  cd \"${target_dir}\""
  echo "  dotnet run --project src/Nexo.CLI -- --help"
  echo "  dotnet run --project src/Nexo.CLI -- validate"
}

main() {
  require_macos
  expand_install_dir

  echo "Nexo macOS one-shot installer"
  echo "  repo-url: ${REPO_URL}"
  echo "  install-dir: ${INSTALL_DIR}"
  if [[ -n "${BRANCH}" ]]; then
    echo "  branch: ${BRANCH}"
  fi

  sync_repo "${INSTALL_DIR}"
  run_setup "${INSTALL_DIR}"
  run_build "${INSTALL_DIR}"
  run_container_smoke "${INSTALL_DIR}"
  print_next_steps "${INSTALL_DIR}"
}

main

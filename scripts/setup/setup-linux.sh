#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# If dotnet was previously installed via dotnet-install.sh, include it in PATH.
export PATH="${HOME}/.dotnet:${HOME}/.dotnet/tools:${PATH}"

MODE="${1:-check}"
shift || true

INCLUDE_OPTIONAL=false
YES=false
FULL_RESTORE=false

usage() {
  echo "Usage: scripts/setup/setup-linux.sh <check|apply|restore|all> [--include-optional] [--yes] [--full-restore]"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --include-optional)
      INCLUDE_OPTIONAL=true
      ;;
    --yes)
      YES=true
      ;;
    --full-restore)
      FULL_RESTORE=true
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

require_linux() {
  if [[ "$(uname -s)" != "Linux" ]]; then
    echo "This script only supports Linux hosts." >&2
    exit 1
  fi
}

run_as_root() {
  if [[ "${EUID}" -eq 0 ]]; then
    "$@"
    return
  fi

  if command -v sudo >/dev/null 2>&1; then
    sudo "$@"
    return
  fi

  echo "Root privileges are required to run: $*" >&2
  return 1
}

pkg_manager() {
  if command -v apt-get >/dev/null 2>&1; then
    echo "apt-get"
    return
  fi
  if command -v dnf >/dev/null 2>&1; then
    echo "dnf"
    return
  fi
  if command -v yum >/dev/null 2>&1; then
    echo "yum"
    return
  fi
  if command -v pacman >/dev/null 2>&1; then
    echo "pacman"
    return
  fi
  if command -v zypper >/dev/null 2>&1; then
    echo "zypper"
    return
  fi
  echo ""
}

install_with_pkg_manager() {
  local manager="$1"
  local package_name="$2"
  case "$manager" in
    apt-get)
      run_as_root apt-get update
      run_as_root apt-get install -y "${package_name}"
      ;;
    dnf)
      run_as_root dnf install -y "${package_name}"
      ;;
    yum)
      run_as_root yum install -y "${package_name}"
      ;;
    pacman)
      run_as_root pacman -Sy --noconfirm "${package_name}"
      ;;
    zypper)
      run_as_root zypper --non-interactive install "${package_name}"
      ;;
    *)
      echo "No supported package manager found for installing ${package_name}." >&2
      return 1
      ;;
  esac
}

has_command() {
  command -v "$1" >/dev/null 2>&1
}

dotnet_major() {
  if ! has_command dotnet; then
    echo "0"
    return
  fi

  local version
  version="$(dotnet --version 2>/dev/null || true)"
  if [[ -z "${version}" ]]; then
    echo "0"
    return
  fi
  echo "${version%%.*}"
}

has_supported_dotnet() {
  local major
  major="$(dotnet_major)"
  [[ "${major}" -ge 9 ]]
}

install_dotnet_user_local() {
  local install_script
  install_script="$(mktemp)"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${install_script}"
  bash "${install_script}" --channel 9.0 --install-dir "${HOME}/.dotnet"
  rm -f "${install_script}"
  export PATH="${HOME}/.dotnet:${HOME}/.dotnet/tools:${PATH}"
  if [[ -x "${HOME}/.dotnet/dotnet" ]]; then
    ln -sf "${HOME}/.dotnet/dotnet" /usr/local/bin/dotnet 2>/dev/null || true
  fi
}

ensure_repo_files() {
  local restore_targets=(
    "src/Nexo.Core.Application/Nexo.Core.Application.csproj"
    "src/Nexo.Infrastructure/Nexo.Infrastructure.csproj"
    "src/Nexo.CLI/Nexo.CLI.csproj"
    "src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj"
    "src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"
  )

  for target in "${restore_targets[@]}"; do
    if [[ ! -f "${REPO_ROOT}/${target}" ]]; then
      echo "Required restore target not found: ${REPO_ROOT}/${target}" >&2
      exit 1
    fi
  done

  if [[ "${FULL_RESTORE}" == "true" && ! -f "${REPO_ROOT}/Nexo.sln" ]]; then
    echo "Nexo.sln not found at ${REPO_ROOT}" >&2
    exit 1
  fi
}

run_restore() {
  ensure_repo_files
  if ! has_command dotnet; then
    echo "dotnet not found. Run apply first." >&2
    return 1
  fi

  if [[ "${FULL_RESTORE}" == "true" ]]; then
    dotnet restore "${REPO_ROOT}/Nexo.sln"
    dotnet restore "${REPO_ROOT}/Nexo.Kernel.sln"
    return
  fi

  dotnet restore "${REPO_ROOT}/src/Nexo.Core.Application/Nexo.Core.Application.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.Infrastructure/Nexo.Infrastructure.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.CLI/Nexo.CLI.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"
}

check_dependencies() {
  local missing_required=()
  local missing_optional=()

  echo "Checking required dependencies (Linux)..."

  if has_command git; then
    echo "  [OK] git"
  else
    echo "  [MISSING] git"
    missing_required+=("git")
  fi

  if has_command curl; then
    echo "  [OK] curl"
  else
    echo "  [MISSING] curl"
    missing_required+=("curl")
  fi

  if has_supported_dotnet; then
    echo "  [OK] dotnet SDK >= 9"
  else
    echo "  [MISSING] dotnet SDK >= 9"
    missing_required+=("dotnet")
  fi

  if has_command docker; then
    echo "  [OK] docker (optional)"
  else
    echo "  [MISSING] docker (optional)"
    missing_optional+=("docker")
  fi

  if has_command ollama; then
    echo "  [OK] ollama (optional)"
  else
    echo "  [MISSING] ollama (optional)"
    missing_optional+=("ollama")
  fi

  if has_command zstd; then
    echo "  [OK] zstd (optional)"
  else
    echo "  [MISSING] zstd (optional)"
    missing_optional+=("zstd")
  fi

  if [[ "${#missing_required[@]}" -gt 0 ]]; then
    echo "Missing required dependencies: ${missing_required[*]}" >&2
    return 1
  fi

  if [[ "${INCLUDE_OPTIONAL}" == "true" && "${#missing_optional[@]}" -gt 0 ]]; then
    echo "Missing optional dependencies (requested): ${missing_optional[*]}" >&2
    return 1
  fi

  echo "Dependency check passed."
}

install_missing_dependencies() {
  local manager
  manager="$(pkg_manager)"

  local missing_required=()
  local missing_optional=()

  if ! has_command git; then missing_required+=("git"); fi
  if ! has_command curl; then missing_required+=("curl"); fi
  if ! has_supported_dotnet; then missing_required+=("dotnet"); fi
  if [[ "${INCLUDE_OPTIONAL}" == "true" ]]; then
    if ! has_command docker; then missing_optional+=("docker"); fi
    if ! has_command ollama; then missing_optional+=("ollama"); fi
    if ! has_command zstd; then missing_optional+=("zstd"); fi
  fi

  if [[ "${#missing_required[@]}" -eq 0 && "${#missing_optional[@]}" -eq 0 ]]; then
    echo "No dependencies to install."
    return 0
  fi

  echo "Install plan:"
  for dep in "${missing_required[@]}"; do
    echo "  - required: ${dep}"
  done
  for dep in "${missing_optional[@]}"; do
    echo "  - optional: ${dep}"
  done

  if [[ "${YES}" != "true" ]]; then
    read -r -p "Proceed with installation? [y/N]: " answer
    if [[ "${answer}" != "y" && "${answer}" != "Y" ]]; then
      echo "Cancelled."
      return 130
    fi
  fi

  for dep in "${missing_required[@]}"; do
    case "${dep}" in
      git)
        install_with_pkg_manager "${manager}" "git"
        ;;
      curl)
        install_with_pkg_manager "${manager}" "curl"
        ;;
      dotnet)
        if [[ "${manager}" == "apt-get" ]]; then
          if ! install_with_pkg_manager "${manager}" "dotnet-sdk-9.0"; then
            echo "Falling back to user-local dotnet-install script."
            install_dotnet_user_local
          fi
        else
          echo "Installing dotnet via user-local installer."
          install_dotnet_user_local
        fi
        ;;
      *)
        echo "Unsupported required dependency: ${dep}" >&2
        return 1
        ;;
    esac
  done

  for dep in "${missing_optional[@]}"; do
    case "${dep}" in
      docker)
        if [[ "${manager}" == "apt-get" ]]; then
          install_with_pkg_manager "${manager}" "docker.io"
        elif [[ "${manager}" == "dnf" || "${manager}" == "yum" ]]; then
          install_with_pkg_manager "${manager}" "docker"
        elif [[ "${manager}" == "pacman" ]]; then
          install_with_pkg_manager "${manager}" "docker"
        elif [[ "${manager}" == "zypper" ]]; then
          install_with_pkg_manager "${manager}" "docker"
        else
          echo "Cannot automatically install docker on this distro." >&2
        fi
        ;;
      ollama)
        curl -fsSL https://ollama.com/install.sh | sh
        ;;
      zstd)
        install_with_pkg_manager "${manager}" "zstd"
        ;;
      *)
        echo "Unsupported optional dependency: ${dep}" >&2
        ;;
    esac
  done
}

main() {
  require_linux
  case "${MODE}" in
    check)
      check_dependencies
      ;;
    apply)
      install_missing_dependencies
      check_dependencies
      ;;
    restore)
      run_restore
      ;;
    all)
      check_dependencies
      run_restore
      ;;
    *)
      echo "Unknown mode: ${MODE}" >&2
      usage
      exit 1
      ;;
  esac
}

main

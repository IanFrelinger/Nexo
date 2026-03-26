#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

MODE="${1:-check}"
shift || true

INCLUDE_OPTIONAL=false
YES=false

usage() {
  echo "Usage: scripts/setup/setup-macos.sh <check|apply|restore|all> [--include-optional] [--yes]"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --include-optional)
      INCLUDE_OPTIONAL=true
      ;;
    --yes)
      YES=true
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

require_macos() {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "This script only supports macOS hosts." >&2
    exit 1
  fi
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

ensure_repo_files() {
  if [[ ! -f "${REPO_ROOT}/src/Nexo.Core.Application/Nexo.Core.Application.csproj" ]]; then
    echo "Nexo.Core.Application.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/src/Nexo.Infrastructure/Nexo.Infrastructure.csproj" ]]; then
    echo "Nexo.Infrastructure.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
    echo "Nexo.CLI.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj" ]]; then
    echo "Nexo.Tests.Infrastructure.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj" ]]; then
    echo "copy-assemblies.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
}

run_restore() {
  ensure_repo_files
  if ! has_command dotnet; then
    echo "dotnet not found. Run apply first." >&2
    return 1
  fi

  dotnet restore "${REPO_ROOT}/src/Nexo.Core.Application/Nexo.Core.Application.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.Infrastructure/Nexo.Infrastructure.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.CLI/Nexo.CLI.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj"
  dotnet restore "${REPO_ROOT}/src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj"
}

check_dependencies() {
  local missing_required=()
  local missing_optional=()

  echo "Checking required dependencies (macOS)..."

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

  if has_command brew; then
    echo "  [OK] homebrew"
  else
    echo "  [MISSING] homebrew"
    missing_required+=("brew")
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

ensure_homebrew() {
  if has_command brew; then
    return 0
  fi

  /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

  if [[ -x /opt/homebrew/bin/brew ]]; then
    eval "$(/opt/homebrew/bin/brew shellenv)"
  elif [[ -x /usr/local/bin/brew ]]; then
    eval "$(/usr/local/bin/brew shellenv)"
  fi
}

install_missing_dependencies() {
  local missing_required=()
  local missing_optional=()

  if ! has_command git; then missing_required+=("git"); fi
  if ! has_command curl; then missing_required+=("curl"); fi
  if ! has_command brew; then missing_required+=("brew"); fi
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

  ensure_homebrew

  for dep in "${missing_required[@]}"; do
    case "${dep}" in
      git)
        brew install git
        ;;
      curl)
        brew install curl
        ;;
      brew)
        # Already handled by ensure_homebrew.
        ;;
      dotnet)
        brew install --cask dotnet-sdk
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
        brew install --cask docker
        ;;
      ollama)
        brew install ollama
        ;;
      zstd)
        brew install zstd
        ;;
      *)
        echo "Unsupported optional dependency: ${dep}" >&2
        ;;
    esac
  done
}

main() {
  require_macos
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

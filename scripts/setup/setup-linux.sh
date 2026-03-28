#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

MODE="${1:-check}"
shift || true

INCLUDE_OPTIONAL=false
FULL_RESTORE=false

usage() {
  echo "Usage: scripts/setup/setup-linux.sh <check|restore|all|apply> [--include-optional] [--full-restore]"
  echo ""
  echo "Notes:"
  echo "  - 'apply' mode is intentionally disabled; install host dependencies via IDE/system package manager."
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --include-optional)
      INCLUDE_OPTIONAL=true
      ;;
    --full-restore)
      FULL_RESTORE=true
      ;;
    --yes)
      # Backward-compatible no-op: this script no longer installs dependencies.
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

  if [[ "${FULL_RESTORE}" == "true" ]]; then
    if [[ ! -f "${REPO_ROOT}/Nexo.sln" || ! -f "${REPO_ROOT}/Nexo.Kernel.sln" ]]; then
      echo "Expected full-restore solution files were not found in ${REPO_ROOT}" >&2
      exit 1
    fi
  fi
}

run_restore() {
  ensure_repo_files
  if ! has_command dotnet; then
    echo "dotnet not found. Install .NET SDK 9+ via your IDE, then re-run setup check/restore." >&2
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

print_missing_guidance() {
  local dep="$1"
  case "${dep}" in
    git) echo "Install Git using your IDE tooling or distro package manager." ;;
    curl) echo "Install curl using your distro package manager." ;;
    dotnet) echo "Install .NET SDK 9+ using your IDE installer (recommended)." ;;
    docker) echo "Install Docker Desktop/Engine manually if you need container workflows." ;;
    ollama) echo "Install Ollama manually if you need local model execution." ;;
    zstd) echo "Install zstd manually if required by your workload." ;;
    *) echo "Install ${dep} manually." ;;
  esac
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
    for dep in "${missing_required[@]}"; do
      echo "  - $(print_missing_guidance "${dep}")" >&2
    done
    return 1
  fi

  if [[ "${INCLUDE_OPTIONAL}" == "true" && "${#missing_optional[@]}" -gt 0 ]]; then
    echo "Missing optional dependencies (requested): ${missing_optional[*]}" >&2
    for dep in "${missing_optional[@]}"; do
      echo "  - $(print_missing_guidance "${dep}")" >&2
    done
    return 1
  fi

  echo "Dependency check passed."
}

disable_apply_mode() {
  echo "Mode 'apply' has been removed." >&2
  echo "This repository no longer auto-installs host dependencies from setup scripts." >&2
  echo "Install prerequisites via your IDE or system package manager, then run:" >&2
  echo "  bash scripts/setup/setup-linux.sh check" >&2
  exit 2
}

main() {
  require_linux
  case "${MODE}" in
    check)
      check_dependencies
      ;;
    apply)
      disable_apply_mode
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

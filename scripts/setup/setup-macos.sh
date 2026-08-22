#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

MODE="check"
if [[ $# -gt 0 ]]; then
  case "$1" in
    check|restore|all|apply)
      MODE="$1"
      shift
      ;;
    -h|--help)
      MODE="help"
      shift
      ;;
  esac
fi

INCLUDE_OPTIONAL=false
AUTO_YES=false
GUIDED=false
TUNE=false

usage() {
  echo "Usage: scripts/setup/setup-macos.sh <check|restore|all|apply> [--include-optional] [--yes] [--guided] [--tune]"
  echo ""
  echo "Notes:"
  echo "  - 'apply' mode installs missing host dependencies where possible."
  echo "  - pass --yes for non-interactive fire-and-forget setup."
  echo "  - pass --tune with 'all' to run the optional Runtime Studio hardware benchmark"
  echo "    (multi-minute Ollama model benchmark; off by default; writes .ashlar/runtime-studio/agent_set.local.json)."
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --include-optional)
      INCLUDE_OPTIONAL=true
      ;;
    --yes)
      AUTO_YES=true
      ;;
    --guided)
      GUIDED=true
      ;;
    --tune)
      TUNE=true
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

if [[ -x /opt/homebrew/bin/brew ]]; then
  eval "$(/opt/homebrew/bin/brew shellenv)"
elif [[ -x /usr/local/bin/brew ]]; then
  eval "$(/usr/local/bin/brew shellenv)"
fi

require_macos() {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "This script only supports macOS hosts." >&2
    exit 1
  fi
}

has_command() {
  command -v "$1" >/dev/null 2>&1
}

confirm_or_exit() {
  local prompt="$1"
  if [[ "${AUTO_YES}" == "true" ]]; then
    return
  fi

  if [[ ! -t 0 ]]; then
    echo "Non-interactive shell detected; re-run with --yes for automatic dependency installation." >&2
    exit 1
  fi

  local response
  read -r -p "${prompt} [y/N] " response
  case "${response}" in
    y|Y|yes|YES)
      ;;
    *)
      echo "Aborted by user."
      exit 1
      ;;
  esac
}

is_ci() {
  [[ -n "${CI:-}" ]]
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
  [[ "${major}" -ge 10 ]]
}

ensure_repo_files() {
  if [[ ! -f "${REPO_ROOT}/src/Ashlar.Core.Application/Ashlar.Core.Application.csproj" ]]; then
    echo "Ashlar.Core.Application.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj" ]]; then
    echo "Ashlar.Infrastructure.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/application/src/Ashlar.CLI/Ashlar.CLI.csproj" ]]; then
    echo "Ashlar.CLI.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj" ]]; then
    echo "Ashlar.Tests.Infrastructure.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
  if [[ ! -f "${REPO_ROOT}/src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj" ]]; then
    echo "copy-assemblies.csproj not found at ${REPO_ROOT}" >&2
    exit 1
  fi
}

run_restore() {
  ensure_repo_files
  if ! has_command dotnet; then
    echo "dotnet not found. Install .NET SDK 10+ via your IDE, then re-run setup check/restore." >&2
    return 1
  fi

  dotnet restore "${REPO_ROOT}/src/Ashlar.Core.Application/Ashlar.Core.Application.csproj"
  dotnet restore "${REPO_ROOT}/src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj"
  dotnet restore "${REPO_ROOT}/application/src/Ashlar.CLI/Ashlar.CLI.csproj"
  dotnet restore "${REPO_ROOT}/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
  dotnet restore "${REPO_ROOT}/src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj"
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
    echo "  [OK] dotnet SDK >= 10"
  else
    echo "  [MISSING] dotnet SDK >= 10"
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

print_missing_guidance() {
  local dep="$1"
  case "${dep}" in
    git) echo "Install Git using Xcode Command Line Tools, Homebrew, or your IDE tooling." ;;
    curl) echo "Install curl using your system package manager." ;;
    brew) echo "Install Homebrew from https://brew.sh/." ;;
    dotnet) echo "Install .NET SDK 10+ using your IDE installer (recommended)." ;;
    docker) echo "Install Docker Desktop manually if you need container workflows." ;;
    ollama) echo "Install Ollama manually if you need local model execution." ;;
    zstd) echo "Install zstd manually if required by your workload." ;;
    *) echo "Install ${dep} manually." ;;
  esac
}

install_xcode_clt_if_needed() {
  if xcode-select -p >/dev/null 2>&1; then
    return
  fi

  if is_ci; then
    echo "Xcode Command Line Tools are required on macOS and were not found." >&2
    echo "Install manually on this CI image before running setup." >&2
    exit 1
  fi

  echo "Installing Xcode Command Line Tools (this may prompt for confirmation)..."
  xcode-select --install >/dev/null 2>&1 || true
  echo "Xcode Command Line Tools installer launched."
  echo "Complete the installation, then re-run this command."
  exit 1
}

install_homebrew_if_needed() {
  if has_command brew; then
    return
  fi

  install_xcode_clt_if_needed
  confirm_or_exit "Homebrew is required to auto-install dependencies. Install Homebrew now?"
  echo "Installing Homebrew..."
  NONINTERACTIVE=1 bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

  if [[ -x /opt/homebrew/bin/brew ]]; then
    eval "$(/opt/homebrew/bin/brew shellenv)"
  elif [[ -x /usr/local/bin/brew ]]; then
    eval "$(/usr/local/bin/brew shellenv)"
  fi

  if ! has_command brew; then
    echo "Homebrew installation did not complete successfully." >&2
    exit 1
  fi
}

brew_install_if_missing() {
  local formula="$1"
  local label="$2"
  local optional="${3:-false}"
  if has_command "${formula}"; then
    echo "  [OK] ${label}${optional:+ (optional)}"
    return
  fi
  echo "  [INSTALL] ${label}${optional:+ (optional)} via Homebrew"
  brew install "${formula}"
}

ensure_dotnet_ready_or_install() {
  if has_supported_dotnet; then
    return
  fi
  echo "  [INSTALL] dotnet SDK >= 10 via Homebrew"
  brew install --cask dotnet-sdk
}

apply_dependencies() {
  if [[ "${GUIDED}" == "true" ]]; then
    echo "============================================="
    echo " Ashlar Setup Assistant (macOS)"
    echo "============================================="
    echo "This setup can install missing prerequisites for you."
    echo "You do not need to know Homebrew commands."
    echo ""
  fi

  echo "Applying required dependencies (macOS)..."
  install_homebrew_if_needed
  if [[ "${AUTO_YES}" != "true" ]]; then
    confirm_or_exit "Ashlar will use Homebrew to install required dependencies (git/curl/dotnet if missing). Continue?"
  fi
  brew update
  brew_install_if_missing git git
  brew_install_if_missing curl curl
  ensure_dotnet_ready_or_install

  if [[ "${INCLUDE_OPTIONAL}" == "true" ]]; then
    if ! has_command docker; then
      echo "  [INFO] Optional dependency docker is not auto-installed on macOS."
      echo "         Install Docker Desktop manually from https://www.docker.com/products/docker-desktop/"
    fi
    if ! has_command ollama; then
      echo "  [INSTALL] ollama (optional) via Homebrew"
      brew install ollama
    fi
    brew_install_if_missing zstd zstd true
  fi

  check_dependencies
}

# Opt-in (--tune). The benchmark runs `ashlar workflow optimize` against local Ollama models for
# several minutes; the tuned ModelName values land in the gitignored
# .ashlar/runtime-studio/agent_set.local.json (seeded from the tracked
# apps/runtime-studio/config/agent_set.local.json), so `setup all` never edits a tracked file.
runtime_studio_auto_tune() {
  if [[ "${TUNE}" != "true" ]]; then
    echo "Runtime Studio hardware tune not requested (pass --tune to run the optional multi-minute Ollama benchmark)."
    return 0
  fi
  if [[ "${ASHLAR_SKIP_RUNTIME_STUDIO_TUNE:-}" == "1" ]]; then
    echo "Skipping Runtime Studio hardware tune (ASHLAR_SKIP_RUNTIME_STUDIO_TUNE=1)."
    return 0
  fi
  if [[ "${CI:-}" == "true" || "${GITHUB_ACTIONS:-}" == "true" ]]; then
    echo "Skipping Runtime Studio hardware tune (CI environment)."
    return 0
  fi

  local tune_script="${REPO_ROOT}/apps/runtime-studio/scripts/optimize_agent_cluster.sh"
  if [[ ! -f "${tune_script}" ]]; then
    return 0
  fi
  if ! has_command dotnet; then
    echo "Skipping Runtime Studio tune (dotnet not found)." >&2
    return 0
  fi

  echo ""
  echo "Runtime Studio: benchmarking local models/compositions (bounded budget). This may take several minutes."
  echo "Tuned agent set is written to .ashlar/runtime-studio/agent_set.local.json (gitignored)."
  echo ""
  (cd "${REPO_ROOT}" && bash "${tune_script}" --skip-daemon --budget-runs 24) \
    || echo "Runtime Studio auto-tune finished with a non-zero exit (optional; re-run the script later)." >&2
}

main() {
  require_macos
  if [[ "${MODE}" == "help" ]]; then
    usage
    exit 0
  fi
  case "${MODE}" in
    check)
      check_dependencies
      ;;
    apply)
      apply_dependencies
      ;;
    restore)
      run_restore
      ;;
    all)
      apply_dependencies
      run_restore
      runtime_studio_auto_tune
      ;;
    *)
      echo "Unknown mode: ${MODE}" >&2
      usage
      exit 1
      ;;
  esac
}

main

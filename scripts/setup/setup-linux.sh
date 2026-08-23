#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

MODE="check"

if [[ $# -gt 0 && "${1}" != --* ]]; then
  MODE="$1"
  shift
fi

INCLUDE_OPTIONAL=false
FULL_RESTORE=false
AUTO_YES=false
GUIDED=false
TUNE=false
PKG_MANAGER=""
APT_UPDATED=false
OS_RELEASE_ID=""
OS_RELEASE_ID_LIKE=""

usage() {
  echo "Usage: scripts/setup/setup-linux.sh <check|restore|all|apply> [--include-optional] [--full-restore] [--tune]"
  echo ""
  echo "Notes:"
  echo "  - 'apply' mode installs missing host dependencies where possible."
  echo "  - pass --yes for non-interactive fire-and-forget setup."
  echo "  - pass --guided to print plain-language setup explanations."
  echo "  - pass --tune with 'all' to run the optional Runtime Studio hardware benchmark"
  echo "    (multi-minute Ollama model benchmark; off by default; writes .ashlar/runtime-studio/agent_set.local.json)."
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

guided_step() {
  local message="$1"
  if [[ "${GUIDED}" == "true" ]]; then
    echo "[guided] ${message}"
  fi
}

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
  [[ "${major}" -ge 10 ]]
}

load_os_release() {
  if [[ -n "${OS_RELEASE_ID}" ]]; then
    return
  fi

  if [[ -f /etc/os-release ]]; then
    OS_RELEASE_ID="$(. /etc/os-release && echo "${ID:-}")"
    OS_RELEASE_ID_LIKE="$(. /etc/os-release && echo "${ID_LIKE:-}")"
  fi
}

persist_homebrew_shellenv() {
  local marker="# Added by Ashlar setup (linuxbrew)"
  local export_line='eval "$(/home/linuxbrew/.linuxbrew/bin/brew shellenv)"'
  local target_profile=""

  for candidate in "${HOME}/.bashrc" "${HOME}/.zshrc" "${HOME}/.profile"; do
    if [[ -f "${candidate}" ]]; then
      target_profile="${candidate}"
      break
    fi
  done

  if [[ -z "${target_profile}" ]]; then
    target_profile="${HOME}/.profile"
  fi

  local marker_present=false
  local line_present=false
  if [[ -f "${target_profile}" ]]; then
    while IFS= read -r line; do
      [[ "${line}" == "${marker}" ]] && marker_present=true
      [[ "${line}" == "${export_line}" ]] && line_present=true
    done < "${target_profile}"
  fi

  if [[ "${line_present}" != "true" ]]; then
    {
      echo ""
      if [[ "${marker_present}" != "true" ]]; then
        echo "${marker}"
      fi
      echo "${export_line}"
    } >> "${target_profile}"
  fi
}

bootstrap_homebrew_linux() {
  if has_command brew; then
    return
  fi

  if ! has_command curl; then
    echo "Cannot bootstrap Homebrew without curl." >&2
    echo "Install curl manually, then re-run setup." >&2
    exit 1
  fi

  confirm_or_exit "No supported package manager detected. Bootstrap Homebrew package manager now?"
  NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

  if [[ -x /home/linuxbrew/.linuxbrew/bin/brew ]]; then
    eval "$(/home/linuxbrew/.linuxbrew/bin/brew shellenv)"
    persist_homebrew_shellenv
  elif [[ -x "${HOME}/.linuxbrew/bin/brew" ]]; then
    eval "$("${HOME}/.linuxbrew/bin/brew" shellenv)"
    persist_homebrew_shellenv
  fi

  if ! has_command brew; then
    echo "Failed to bootstrap Homebrew automatically." >&2
    echo "Install a system package manager (apt/dnf/yum/pacman/zypper/apk) or Homebrew, then retry." >&2
    exit 1
  fi
}

ensure_package_manager() {
  if [[ -n "${PKG_MANAGER}" ]]; then
    return
  fi

  if has_command apt-get; then
    PKG_MANAGER="apt"
  elif has_command dnf; then
    PKG_MANAGER="dnf"
  elif has_command yum; then
    PKG_MANAGER="yum"
  elif has_command pacman; then
    PKG_MANAGER="pacman"
  elif has_command zypper; then
    PKG_MANAGER="zypper"
  elif has_command apk; then
    PKG_MANAGER="apk"
  elif has_command brew; then
    PKG_MANAGER="brew"
  else
    load_os_release
    echo "No supported Linux package manager found (apt/dnf/yum/pacman/zypper/apk)." >&2
    if [[ -n "${OS_RELEASE_ID}" ]]; then
      echo "Detected distro ID: ${OS_RELEASE_ID}${OS_RELEASE_ID_LIKE:+ (like: ${OS_RELEASE_ID_LIKE})}" >&2
    fi
    bootstrap_homebrew_linux
    PKG_MANAGER="brew"
  fi
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

run_privileged() {
  if [[ "${EUID}" -eq 0 ]]; then
    "$@"
    return
  fi

  if has_command sudo; then
    sudo "$@"
    return
  fi

  echo "Root privileges are required for system package installation (sudo not found)." >&2
  exit 1
}

linux_package_name() {
  local dep="$1"
  ensure_package_manager

  case "${dep}" in
    git) echo "git" ;;
    curl) echo "curl" ;;
    zstd) echo "zstd" ;;
    docker)
      case "${PKG_MANAGER}" in
        apt) echo "docker.io" ;;
        dnf|yum|pacman|zypper|apk|brew) echo "docker" ;;
      esac
      ;;
    *)
      echo ""
      ;;
  esac
}

install_system_dependency() {
  local dep="$1"
  local package_name
  package_name="$(linux_package_name "${dep}")"
  if [[ -z "${package_name}" ]]; then
    echo "No package mapping found for dependency '${dep}' on this distribution." >&2
    return 1
  fi

  confirm_or_exit "Install missing dependency '${dep}' using ${PKG_MANAGER}?"

  case "${PKG_MANAGER}" in
    apt)
      if [[ "${APT_UPDATED}" != "true" ]]; then
        run_privileged apt-get update
        APT_UPDATED=true
      fi
      run_privileged apt-get install -y "${package_name}"
      ;;
    dnf)
      run_privileged dnf install -y "${package_name}"
      ;;
    yum)
      run_privileged yum install -y "${package_name}"
      ;;
    pacman)
      run_privileged pacman -Sy --noconfirm "${package_name}"
      ;;
    zypper)
      run_privileged zypper --non-interactive install "${package_name}"
      ;;
    apk)
      run_privileged apk add --no-cache "${package_name}"
      ;;
    brew)
      brew install "${package_name}"
      ;;
    *)
      echo "Unsupported package manager: ${PKG_MANAGER}" >&2
      return 1
      ;;
  esac
}

ensure_dotnet_path() {
  export PATH="${HOME}/.dotnet:${HOME}/.dotnet/tools:${PATH}"

  local marker="# Added by Ashlar setup (dotnet)"
  local export_line='export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"'
  local target_profile=""

  for candidate in "${HOME}/.bashrc" "${HOME}/.zshrc" "${HOME}/.profile"; do
    if [[ -f "${candidate}" ]]; then
      target_profile="${candidate}"
      break
    fi
  done

  if [[ -z "${target_profile}" ]]; then
    target_profile="${HOME}/.profile"
  fi

  local marker_present=false
  local line_present=false

  if [[ -f "${target_profile}" ]]; then
    while IFS= read -r line; do
      [[ "${line}" == "${marker}" ]] && marker_present=true
      [[ "${line}" == "${export_line}" ]] && line_present=true
    done < "${target_profile}"
  fi

  if [[ "${line_present}" != "true" ]]; then
    {
      echo ""
      if [[ "${marker_present}" != "true" ]]; then
        echo "${marker}"
      fi
      echo "${export_line}"
    } >> "${target_profile}"
  fi
}

install_dotnet_sdk() {
  if has_supported_dotnet; then
    return
  fi

  ensure_package_manager
  if [[ "${PKG_MANAGER}" == "brew" ]]; then
    confirm_or_exit "Install .NET SDK 10.x using Homebrew?"
    brew install dotnet-sdk
    ensure_dotnet_path
    if ! has_supported_dotnet; then
      echo "Failed to install .NET SDK 10.x via Homebrew." >&2
      return 1
    fi
    return
  fi

  if ! has_command curl; then
    install_system_dependency "curl"
  fi

  confirm_or_exit "Install .NET SDK 10.x to ${HOME}/.dotnet?"

  local installer
  installer="$(mktemp)"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${installer}"
  bash "${installer}" --channel 10.0 --install-dir "${HOME}/.dotnet"
  rm -f "${installer}"

  ensure_dotnet_path

  if ! has_supported_dotnet; then
    echo "Failed to install .NET SDK 10.x automatically." >&2
    return 1
  fi
}

install_optional_dependency() {
  local dep="$1"
  case "${dep}" in
    docker|zstd)
      install_system_dependency "${dep}"
      ;;
    ollama)
      if has_command ollama; then
        return
      fi
      if ! has_command curl; then
        install_system_dependency "curl"
      fi
      confirm_or_exit "Install optional dependency 'ollama'?"
      curl -fsSL https://ollama.com/install.sh | sh
      ;;
    *)
      return
      ;;
  esac
}

ensure_repo_files() {
  local restore_targets=(
    "src/Ashlar.Core.Application/Ashlar.Core.Application.csproj"
    "src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj"
    "application/src/Ashlar.CLI/Ashlar.CLI.csproj"
    "src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj"
    "src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
  )

  for target in "${restore_targets[@]}"; do
    if [[ ! -f "${REPO_ROOT}/${target}" ]]; then
      echo "Required restore target not found: ${REPO_ROOT}/${target}" >&2
      exit 1
    fi
  done

  if [[ "${FULL_RESTORE}" == "true" ]]; then
    if [[ ! -f "${REPO_ROOT}/Ashlar.sln" || ! -f "${REPO_ROOT}/Ashlar.Kernel.sln" ]]; then
      echo "Expected full-restore solution files were not found in ${REPO_ROOT}" >&2
      exit 1
    fi
  fi
}

run_restore() {
  ensure_repo_files
  if ! has_supported_dotnet; then
    echo "dotnet SDK 10+ not found. Run apply mode first: bash scripts/setup/setup-linux.sh apply --yes" >&2
    return 1
  fi

  if [[ "${FULL_RESTORE}" == "true" ]]; then
    dotnet restore "${REPO_ROOT}/Ashlar.sln"
    dotnet restore "${REPO_ROOT}/Ashlar.Kernel.sln"
    return
  fi

  dotnet restore "${REPO_ROOT}/src/Ashlar.Core.Application/Ashlar.Core.Application.csproj"
  dotnet restore "${REPO_ROOT}/src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj"
  dotnet restore "${REPO_ROOT}/application/src/Ashlar.CLI/Ashlar.CLI.csproj"
  dotnet restore "${REPO_ROOT}/src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj"
  dotnet restore "${REPO_ROOT}/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
}

print_missing_guidance() {
  local dep="$1"
  case "${dep}" in
    git) echo "Run: bash scripts/setup/setup-linux.sh apply --yes (auto-installs git)." ;;
    curl) echo "Run: bash scripts/setup/setup-linux.sh apply --yes (auto-installs curl)." ;;
    dotnet) echo "Run: bash scripts/setup/setup-linux.sh apply --yes (auto-installs .NET SDK 10 locally)." ;;
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

apply_dependencies() {
  guided_step "Checking and installing required developer tools (git, curl, .NET SDK 10+) if missing."
  if ! has_command git; then
    guided_step "Git is missing; installing it now."
    install_system_dependency "git"
  fi
  if ! has_command curl; then
    guided_step "curl is missing; installing it now."
    install_system_dependency "curl"
  fi
  if ! has_supported_dotnet; then
    guided_step ".NET SDK 10 is missing; installing it now."
    install_dotnet_sdk
  fi

  if [[ "${INCLUDE_OPTIONAL}" == "true" ]]; then
    guided_step "Optional dependencies were requested; installing what is missing."
    if ! has_command docker; then
      install_optional_dependency "docker"
    fi
    if ! has_command ollama; then
      install_optional_dependency "ollama"
    fi
    if ! has_command zstd; then
      install_optional_dependency "zstd"
    fi
  fi

  guided_step "Dependency bootstrap finished. Running final verification checks."
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
  require_linux
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
